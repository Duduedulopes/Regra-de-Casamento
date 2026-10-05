using RegraDeCasamento.Domain.Comum;
using RegraDeCasamento.Domain.Familias;

namespace RegraDeCasamento.Domain.Tests.Familias;

public class CriancaTests
{
    [Fact]
    public void R35_AdultoCriaAContaDaCrianca_IdentificadaPeloApelido()
    {
        var familia = Familia.Criar("Família Silva", "Ana");
        var ana = familia.Membros.Single();

        var resultado = familia.AdicionarCrianca(ana.Id, "Lia Silva", "lia");

        Assert.True(resultado.Sucesso);
        Assert.Equal(PerfilDoMembro.Crianca, resultado.Valor.Perfil);
        Assert.Equal("Lia Silva", resultado.Valor.Nome);
        Assert.Equal("lia", resultado.Valor.Apelido);
        Assert.Contains(resultado.Valor, familia.Membros);
    }

    [Fact]
    public void R35_CriancaNaoCriaContaDeOutraCrianca()
    {
        var familia = Familia.Criar("Família Silva", "Ana");
        var lia = familia.AdicionarCrianca(familia.Membros.Single().Id, "Lia", "lia").Valor;

        var resultado = familia.AdicionarCrianca(lia.Id, "Theo", "theo");

        Assert.Equal("R35", resultado.CodigoDaRegra);
        Assert.Equal(2, familia.Membros.Count);
    }

    [Theory]
    [InlineData("lia")]
    [InlineData("LIA")]
    [InlineData(" Lia ")]
    public void R35_ApelidoRepetidoNaFamilia_NaoPode(string apelidoRepetido)
    {
        var familia = Familia.Criar("Família Silva", "Ana");
        var ana = familia.Membros.Single();
        familia.AdicionarCrianca(ana.Id, "Lia", "lia");

        var resultado = familia.AdicionarCrianca(ana.Id, "Outra Lia", apelidoRepetido);

        Assert.Equal("R35", resultado.CodigoDaRegra);
        Assert.Equal(2, familia.Membros.Count);
    }

    [Fact]
    public void R35_MesmoApelidoEmOutraFamilia_Pode()
    {
        var silva = Familia.Criar("Família Silva", "Ana");
        var souza = Familia.Criar("Família Souza", "Paula");
        silva.AdicionarCrianca(silva.Membros.Single().Id, "Lia", "lia");

        var resultado = souza.AdicionarCrianca(souza.Membros.Single().Id, "Lia", "lia");

        Assert.True(resultado.Sucesso);
    }

    [Theory]
    [InlineData("lia silva")]
    [InlineData("lia@casa")]
    [InlineData("lia!")]
    public void Apelido_SoComLetrasNumerosPontoHifenESublinhado(string apelido)
    {
        var familia = Familia.Criar("Família Silva", "Ana");

        var erro = Assert.Throws<DadosInvalidosException>(() => familia.AdicionarCrianca(familia.Membros.Single().Id, "Lia", apelido));

        Assert.Contains("apelido", erro.Erros.Single());
    }

    [Theory]
    [InlineData("joão")]
    [InlineData("lia.2")]
    [InlineData("mia_b-1")]
    public void Apelido_ComAcentoPontoHifenOuSublinhado_Pode(string apelido)
    {
        var familia = Familia.Criar("Família Silva", "Ana");

        Assert.True(familia.AdicionarCrianca(familia.Membros.Single().Id, "Criança", apelido).Sucesso);
    }

    [Fact]
    public void DadoInvalido_TemMensagemEmPortugues()
    {
        var erro = Assert.Throws<DadosInvalidosException>(() => Familia.Criar(" ", "Ana"));

        Assert.Equal("Preencha o nome da família.", Assert.Single(erro.Erros));
    }

    [Theory]
    [InlineData("", "lia")]
    [InlineData("Lia", "")]
    [InlineData("Lia", "um-apelido-grande-demais-para-caber-aqui")]
    public void Crianca_SemNomeOuApelidoValido_EErroDeQuemChama(string nome, string apelido)
    {
        var familia = Familia.Criar("Família Silva", "Ana");

        Assert.ThrowsAny<ArgumentException>(() => familia.AdicionarCrianca(familia.Membros.Single().Id, nome, apelido));
    }
}
