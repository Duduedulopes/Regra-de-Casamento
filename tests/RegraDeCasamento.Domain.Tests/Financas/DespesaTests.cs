using RegraDeCasamento.Domain.Comum;
using RegraDeCasamento.Domain.Familias;
using RegraDeCasamento.Domain.Financas;

namespace RegraDeCasamento.Domain.Tests.Financas;

public class DespesaTests
{
    private readonly Casal _casal = new();

    private Despesa Aluguel(Guid? donoId = null) =>
        Despesa.Lancar(_casal.Familia, _casal.Ana.Id, "Aluguel", 1500m, Casal.Dia(10), donoId).Valor;

    [Fact]
    public void R6_DespesaSemDono_EDaCasa()
    {
        var aluguel = Aluguel();

        Assert.True(aluguel.EhDaCasa);
        Assert.Null(aluguel.DonoId);
        Assert.Equal(_casal.Familia.Id, aluguel.FamiliaId);
    }

    [Fact]
    public void R6_DespesaPodeSerDeUmAdulto()
    {
        var pensao = Despesa.Lancar(_casal.Familia, _casal.Bruno.Id, "Pensão paga", 900m, Casal.Dia(10), _casal.Bruno.Id).Valor;

        Assert.False(pensao.EhDaCasa);
        Assert.Equal(_casal.Bruno.Id, pensao.DonoId);
    }

    [Fact]
    public void R37_AdultoNaoLancaDespesaNoNomeDoOutro()
    {
        var resultado = Despesa.Lancar(_casal.Familia, _casal.Ana.Id, "Pensão paga", 900m, Casal.Dia(10), _casal.Bruno.Id);

        Assert.Equal("R37", resultado.CodigoDaRegra);
    }

    [Fact]
    public void R37_SoODonoMarcaComoPagaOuDesfaz()
    {
        var pensao = Despesa.Lancar(_casal.Familia, _casal.Bruno.Id, "Pensão paga", 900m, Casal.Dia(10), _casal.Bruno.Id).Valor;

        var pelaAna = pensao.MarcarComoPaga(_casal.Familia, _casal.Ana.Id, _casal.Bruno.Id, Casal.Dia(10));
        var peloBruno = pensao.MarcarComoPaga(_casal.Familia, _casal.Bruno.Id, _casal.Bruno.Id, Casal.Dia(10));
        var desfazerPelaAna = pensao.MarcarComoAPagar(_casal.Familia, _casal.Ana.Id);

        Assert.Equal("R37", pelaAna.CodigoDaRegra);
        Assert.True(peloBruno.Sucesso);
        Assert.Equal("R37", desfazerPelaAna.CodigoDaRegra);
        Assert.Equal(SituacaoDaDespesa.Paga, pensao.Situacao);
    }

    [Fact]
    public void R37_ContaDaCasa_OsDoisAlteram()
    {
        var aluguel = Aluguel();

        Assert.True(aluguel.MarcarComoPaga(_casal.Familia, _casal.Bruno.Id, _casal.Bruno.Id, Casal.Dia(9)).Sucesso);
        Assert.True(aluguel.MarcarComoAPagar(_casal.Familia, _casal.Ana.Id).Sucesso);
    }

    [Fact]
    public void R6_CriancaNaoEDonaDeDespesa()
    {
        var resultado = Despesa.Lancar(_casal.Familia, _casal.Ana.Id, "Escola", 800m, Casal.Dia(5), _casal.Lia.Id);

        Assert.Equal("R6", resultado.CodigoDaRegra);
    }

    [Fact]
    public void R6_DonoDeOutraFamilia_NaoPode()
    {
        var deOutraFamilia = Familia.Criar("Família Souza", "Paula").Membros.Single();

        var resultado = Despesa.Lancar(_casal.Familia, _casal.Ana.Id, "Luz", 200m, Casal.Dia(5), deOutraFamilia.Id);

        Assert.Equal("R6", resultado.CodigoDaRegra);
    }

    [Fact]
    public void R4_CriancaNaoLancaDespesa()
    {
        var resultado = Despesa.Lancar(_casal.Familia, _casal.Lia.Id, "Sorvete", 10m, Casal.Dia(5), null);

        Assert.Equal("R4", resultado.CodigoDaRegra);
    }

    [Fact]
    public void R5_AdultoDeOutraFamilia_NaoLancaDespesaAqui()
    {
        var paula = Familia.Criar("Família Souza", "Paula").Membros.Single();

        var resultado = Despesa.Lancar(_casal.Familia, paula.Id, "Luz", 200m, Casal.Dia(5), null);

        Assert.Equal("R5", resultado.CodigoDaRegra);
    }

    [Fact]
    public void R10_DespesaNasceAPagar()
    {
        var aluguel = Aluguel();

        Assert.Equal(SituacaoDaDespesa.APagar, aluguel.Situacao);
        Assert.Null(aluguel.PagaPorId);
        Assert.Null(aluguel.PagaEm);
    }

    [Fact]
    public void R8_MarcarComoPaga_GuardaQuemPagouEQuando()
    {
        var aluguel = Aluguel();

        var resultado = aluguel.MarcarComoPaga(_casal.Familia, _casal.Ana.Id, _casal.Bruno.Id, Casal.Dia(9));

        Assert.True(resultado.Sucesso);
        Assert.Equal(SituacaoDaDespesa.Paga, aluguel.Situacao);
        Assert.Equal(_casal.Bruno.Id, aluguel.PagaPorId);
        Assert.Equal(Casal.Dia(9), aluguel.PagaEm);
    }

    [Fact]
    public void R8_QuemPagouPrecisaSerAdultoDaFamilia()
    {
        var aluguel = Aluguel();

        var resultado = aluguel.MarcarComoPaga(_casal.Familia, _casal.Ana.Id, _casal.Lia.Id, Casal.Dia(9));

        Assert.Equal("R8", resultado.CodigoDaRegra);
        Assert.Equal(SituacaoDaDespesa.APagar, aluguel.Situacao);
    }

    [Fact]
    public void R10_ContaPagaNaoEPagaDeNovo()
    {
        var aluguel = Aluguel();
        aluguel.MarcarComoPaga(_casal.Familia, _casal.Ana.Id, _casal.Ana.Id, Casal.Dia(9));

        var resultado = aluguel.MarcarComoPaga(_casal.Familia, _casal.Ana.Id, _casal.Bruno.Id, Casal.Dia(10));

        Assert.Equal("R10", resultado.CodigoDaRegra);
        Assert.Equal(_casal.Ana.Id, aluguel.PagaPorId);
    }

    [Fact]
    public void R10_ContaPagaPorEngano_VoltaParaAPagar()
    {
        var aluguel = Aluguel();
        aluguel.MarcarComoPaga(_casal.Familia, _casal.Ana.Id, _casal.Ana.Id, Casal.Dia(9));

        var resultado = aluguel.MarcarComoAPagar(_casal.Familia, _casal.Ana.Id);

        Assert.True(resultado.Sucesso);
        Assert.Equal(SituacaoDaDespesa.APagar, aluguel.Situacao);
        Assert.Null(aluguel.PagaPorId);
        Assert.Null(aluguel.PagaEm);
    }

    [Fact]
    public void R5_ContaDeOutraFamilia_NaoEPagaComEstaFamilia()
    {
        var outro = new Casal();
        var contaDeOutraFamilia = Despesa.Lancar(outro.Familia, outro.Ana.Id, "Luz", 100m, Casal.Dia(5), null).Valor;

        var resultado = contaDeOutraFamilia.MarcarComoPaga(_casal.Familia, _casal.Ana.Id, _casal.Ana.Id, Casal.Dia(5));

        Assert.Equal("R5", resultado.CodigoDaRegra);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(0.004)]
    public void ValorZeroOuNegativo_EDadoInvalido(decimal valor)
    {
        Assert.Throws<DadosInvalidosException>(() => Despesa.Lancar(_casal.Familia, _casal.Ana.Id, "Luz", valor, Casal.Dia(5), null));
    }

    [Fact]
    public void Valor_FicaComDuasCasas()
    {
        var luz = Despesa.Lancar(_casal.Familia, _casal.Ana.Id, "Luz", 199.999m, Casal.Dia(5), null).Valor;

        Assert.Equal(200.00m, luz.Valor);
    }
}
