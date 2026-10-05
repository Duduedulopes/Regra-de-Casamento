using RegraDeCasamento.Application.Familias;
using RegraDeCasamento.Shared.Acesso;
using RegraDeCasamento.Shared.Familias;

namespace RegraDeCasamento.Application.Tests.Familias;

public class CriarFamiliaTests
{
    private readonly UsuarioFixo _usuario = new();
    private readonly ContasEmMemoria _contas = new();
    private readonly UnidadeDeTrabalhoQueConta _unidade = new();
    private readonly FamiliasEmMemoria _familias;

    public CriarFamiliaTests() => _familias = new FamiliasEmMemoria(_usuario);

    private CriarFamilia CasoDeUso() => new(_usuario, _contas, _familias, _unidade);

    [Fact]
    public async Task QuemCriaAFamilia_ViraOPrimeiroAdulto_EAContaFicaLigadaAEle()
    {
        var resultado = await CasoDeUso().ExecutarAsync(new DadosDaNovaFamilia("Família Silva", "Ana"));

        Assert.True(resultado.Sucesso);
        var adulto = Assert.Single(resultado.Valor.Membros);
        Assert.Equal(PerfilDeAcesso.Adulto, adulto.Perfil);
        Assert.Equal(adulto.Id, _contas.MembroDaConta[_usuario.ContaId!.Value]);
        Assert.Single(_familias.Familias);
        Assert.Equal(1, _unidade.VezesQueSalvou);
    }

    [Fact]
    public async Task R1_ContaQueJaTemFamilia_NaoCriaOutra()
    {
        _contas.MembroDaConta[_usuario.ContaId!.Value] = Guid.CreateVersion7();

        var resultado = await CasoDeUso().ExecutarAsync(new DadosDaNovaFamilia("Família Silva", "Ana"));

        Assert.Equal("R1", resultado.CodigoDaRegra);
        Assert.Empty(_familias.Familias);
        Assert.Equal(0, _unidade.VezesQueSalvou);
    }
}
