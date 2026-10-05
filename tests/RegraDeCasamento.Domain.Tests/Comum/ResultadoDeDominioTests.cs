using RegraDeCasamento.Domain.Comum;

namespace RegraDeCasamento.Domain.Tests.Comum;

public class ResultadoDeDominioTests
{
    [Fact]
    public void Ok_DeuCerto_SemCodigoESemMensagem()
    {
        var resultado = ResultadoDeDominio.Ok();

        Assert.True(resultado.Sucesso);
        Assert.False(resultado.Falhou);
        Assert.Null(resultado.CodigoDaRegra);
        Assert.Null(resultado.Mensagem);
    }

    [Fact]
    public void Falha_DevolveOCodigoDaRegraEAMensagem()
    {
        var resultado = ResultadoDeDominio.Falha("R18", "A troca só vale com aceite.");

        Assert.True(resultado.Falhou);
        Assert.False(resultado.Sucesso);
        Assert.Equal("R18", resultado.CodigoDaRegra);
        Assert.Equal("A troca só vale com aceite.", resultado.Mensagem);
    }

    [Fact]
    public void OkComValor_EntregaOValor()
    {
        var resultado = ResultadoDeDominio.Ok(42);

        Assert.True(resultado.Sucesso);
        Assert.Equal(42, resultado.Valor);
    }

    [Fact]
    public void FalhaComValor_NaoTemValorParaEntregar()
    {
        var resultado = ResultadoDeDominio.Falha<int>("R1", "A família já tem os dois adultos.");

        Assert.True(resultado.Falhou);
        Assert.Equal("R1", resultado.CodigoDaRegra);
        Assert.Throws<InvalidOperationException>(() => resultado.Valor);
    }

    [Fact]
    public void RepassarFalha_MantemOCodigoEAMensagem()
    {
        var original = ResultadoDeDominio.Falha<string>("R34", "Este pedido já foi respondido.");

        var repassado = original.RepassarFalha<int>();

        Assert.True(repassado.Falhou);
        Assert.Equal("R34", repassado.CodigoDaRegra);
        Assert.Equal("Este pedido já foi respondido.", repassado.Mensagem);
    }

    [Fact]
    public void RepassarFalha_DeUmSucesso_EErroDeProgramacao()
    {
        var sucesso = ResultadoDeDominio.Ok();

        Assert.Throws<InvalidOperationException>(() => sucesso.RepassarFalha<int>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("R")]
    [InlineData("18")]
    [InlineData("r18")]
    [InlineData("R18a")]
    [InlineData("X18")]
    [InlineData(" R18")]
    public void Falha_ComCodigoForaDoFormato_EErroDeProgramacao(string codigo)
    {
        Assert.Throws<ArgumentException>(() => ResultadoDeDominio.Falha(codigo, "Mensagem."));
        Assert.Throws<ArgumentException>(() => ResultadoDeDominio.Falha<int>(codigo, "Mensagem."));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Falha_SemMensagem_EErroDeProgramacao(string mensagem)
    {
        Assert.Throws<ArgumentException>(() => ResultadoDeDominio.Falha("R1", mensagem));
    }
}
