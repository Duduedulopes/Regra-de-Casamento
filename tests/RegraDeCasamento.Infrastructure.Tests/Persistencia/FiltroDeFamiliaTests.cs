using Microsoft.EntityFrameworkCore;
using RegraDeCasamento.Infrastructure.Familias;

namespace RegraDeCasamento.Infrastructure.Tests.Persistencia;

public sealed class FiltroDeFamiliaTests : IDisposable
{
    private readonly BancoEmMemoria _banco = new();

    public void Dispose() => _banco.Dispose();

    [Fact]
    public async Task R5_FamiliaANaoEnxergaNadaDaFamiliaB()
    {
        var familiaA = await _banco.GravarAsync(BancoEmMemoria.MontarFamilia("Família A", "Ana", "lia"));
        var familiaB = await _banco.GravarAsync(BancoEmMemoria.MontarFamilia("Família B", "Bia", "theo"));

        await using var comoA = _banco.ContextoDaFamilia(familiaA.Id);

        var familias = await comoA.Familias.ToListAsync();
        var membros = await comoA.Membros.ToListAsync();
        var pedidos = await comoA.PedidosDeEntrada.ToListAsync();

        Assert.Equal(familiaA.Id, Assert.Single(familias).Id);
        Assert.Equal(2, membros.Count);
        Assert.All(membros, membro => Assert.Equal(familiaA.Id, membro.FamiliaId));
        Assert.Equal(familiaA.Id, Assert.Single(pedidos).FamiliaId);

        // Nem pedindo pelo Id da família B ela aparece.
        Assert.False(await comoA.Familias.AnyAsync(familia => familia.Id == familiaB.Id));
        Assert.False(await comoA.Membros.AnyAsync(membro => membro.FamiliaId == familiaB.Id));
        Assert.False(await comoA.PedidosDeEntrada.AnyAsync(pedido => pedido.FamiliaId == familiaB.Id));
    }

    [Fact]
    public async Task R5_CadaContextoUsaAFamiliaDeQuemEstaLogado()
    {
        // O modelo do EF é montado uma vez só; o filtro precisa ler a família do contexto que está consultando.
        var familiaA = await _banco.GravarAsync(BancoEmMemoria.MontarFamilia("Família A", "Ana", "lia"));
        var familiaB = await _banco.GravarAsync(BancoEmMemoria.MontarFamilia("Família B", "Bia", "theo"));

        await using var comoA = _banco.ContextoDaFamilia(familiaA.Id);
        await using var comoB = _banco.ContextoDaFamilia(familiaB.Id);

        Assert.Equal("Família A", (await comoA.Familias.SingleAsync()).Nome);
        Assert.Equal("Família B", (await comoB.Familias.SingleAsync()).Nome);
        Assert.Equal("Família A", (await comoA.Familias.SingleAsync()).Nome);
    }

    [Fact]
    public async Task R5_SemFamiliaLogada_NaoEnxergaNada()
    {
        await _banco.GravarAsync(BancoEmMemoria.MontarFamilia("Família A", "Ana", "lia"));

        await using var semLogin = _banco.ContextoDaFamilia(null);

        Assert.False(await semLogin.Familias.AnyAsync());
        Assert.False(await semLogin.Membros.AnyAsync());
        Assert.False(await semLogin.PedidosDeEntrada.AnyAsync());
    }

    [Fact]
    public async Task R5_RepositorioDevolveSoAFamiliaLogada_ComMembrosEPedidos()
    {
        var familiaA = await _banco.GravarAsync(BancoEmMemoria.MontarFamilia("Família A", "Ana", "lia"));
        await _banco.GravarAsync(BancoEmMemoria.MontarFamilia("Família B", "Bia", "theo"));

        await using var comoA = _banco.ContextoDaFamilia(familiaA.Id);
        var familia = await new RepositorioDeFamilias(comoA).ObterDaFamiliaAtualAsync();

        Assert.NotNull(familia);
        Assert.Equal(familiaA.Id, familia.Id);
        Assert.Equal(2, familia.Membros.Count);
        Assert.Single(familia.PedidosDeEntrada);
    }

    [Fact]
    public async Task IdsEDatas_VoltamDoBancoComoForamCriados()
    {
        var original = await _banco.GravarAsync(BancoEmMemoria.MontarFamilia("Família A", "Ana", "lia"));

        await using var comoA = _banco.ContextoDaFamilia(original.Id);
        var lida = await comoA.Familias.Include(familia => familia.Membros).SingleAsync();

        Assert.Equal(original.Id, lida.Id);
        Assert.Equal(original.Codigo, lida.Codigo);
        Assert.Equal(original.Membros.Select(m => m.Id).Order(), lida.Membros.Select(m => m.Id).Order());
    }
}
