using RegraDeCasamento.Application.Familias;
using RegraDeCasamento.Domain.Comum;
using RegraDeCasamento.Domain.Familias;
using RegraDeCasamento.Shared.Familias;

namespace RegraDeCasamento.Application.Tests.Familias;

public class AdicionarCriancaTests
{
    private readonly UsuarioFixo _usuario = new();
    private readonly ContasEmMemoria _contas = new() { SenhaRecusada = "12" };
    private readonly FamiliasEmMemoria _familias;
    private readonly Familia _familia = Familia.Criar("Família Silva", "Ana");

    public AdicionarCriancaTests()
    {
        _familias = new FamiliasEmMemoria(_usuario);
        _familias.Adicionar(_familia);
        _usuario.MembroId = _familia.Membros.Single().Id;
    }

    private AdicionarCrianca CasoDeUso() => new(_usuario, _contas, _familias);

    [Fact]
    public async Task R35_CriaACriancaEAContaComApelidoECodigo()
    {
        var resultado = await CasoDeUso().ExecutarAsync(new DadosDaNovaCrianca("Lia", "lia", "1234"));

        Assert.True(resultado.Sucesso);
        Assert.Equal($"lia@{_familia.Codigo}", resultado.Valor.Usuario);
        var conta = Assert.Single(_contas.ContasCriadas);
        Assert.Equal(resultado.Valor.MembroId, conta.MembroId);
    }

    [Fact]
    public async Task R35_ApelidoRepetido_NaoCriaConta()
    {
        await CasoDeUso().ExecutarAsync(new DadosDaNovaCrianca("Lia", "lia", "1234"));

        var resultado = await CasoDeUso().ExecutarAsync(new DadosDaNovaCrianca("Outra", "lia", "1234"));

        Assert.Equal("R35", resultado.CodigoDaRegra);
        Assert.Single(_contas.ContasCriadas);
    }

    [Fact]
    public async Task PinRecusado_EDadoInvalido()
    {
        await Assert.ThrowsAsync<DadosInvalidosException>(() => CasoDeUso().ExecutarAsync(new DadosDaNovaCrianca("Lia", "lia", "12")));
        Assert.Empty(_contas.ContasCriadas);
    }
}
