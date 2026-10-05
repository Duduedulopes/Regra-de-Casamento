using RegraDeCasamento.Domain.Comum;
using RegraDeCasamento.Domain.Financas;

namespace RegraDeCasamento.Domain.Tests.Financas;

public class DividaTests
{
    private readonly Casal _casal = new();

    private NovaDivida Cartao(decimal total = 1000m, int parcelas = 3, Guid? donoId = null) =>
        new("Geladeira no cartão", TipoDeDivida.CompraParcelada, total, parcelas, Casal.Dia(10), donoId ?? _casal.Ana.Id);

    [Fact]
    public void R38_CadaParcelaViraUmaContaAPagarNoMesDela()
    {
        var lancada = Divida.Lancar(_casal.Familia, _casal.Ana.Id, Cartao()).Valor;

        Assert.Equal(3, lancada.Parcelas.Count);
        Assert.Equal([Casal.Dia(10), Casal.Dia(10, mes: 11), Casal.Dia(10, mes: 12)], lancada.Parcelas.Select(p => p.Vencimento));
        Assert.All(lancada.Parcelas, parcela =>
        {
            Assert.Equal(SituacaoDaDespesa.APagar, parcela.Situacao);
            Assert.Equal(lancada.Divida.Id, parcela.DividaId);
            Assert.Equal(_casal.Ana.Id, parcela.DonoId);
        });
        Assert.Equal("Geladeira no cartão (2/3)", lancada.Parcelas[1].Descricao);
    }

    [Fact]
    public void R38_AsParcelasSomamOValorTotal_AUltimaLevaOsCentavos()
    {
        var lancada = Divida.Lancar(_casal.Familia, _casal.Ana.Id, Cartao(total: 1000m, parcelas: 3)).Valor;

        Assert.Equal([333.33m, 333.33m, 333.34m], lancada.Parcelas.Select(p => p.Valor));
        Assert.Equal(1000m, lancada.Parcelas.Sum(p => p.Valor));
    }

    [Fact]
    public void R38_AndamentoMostraQuantoFoiPagoEQuantoFalta()
    {
        var lancada = Divida.Lancar(_casal.Familia, _casal.Ana.Id, Cartao(total: 900m, parcelas: 3)).Valor;
        lancada.Parcelas[0].MarcarComoPaga(_casal.Familia, _casal.Ana.Id, _casal.Ana.Id, Casal.Dia(10));

        var andamento = AndamentoDaDivida.Calcular(lancada.Divida, lancada.Parcelas);

        Assert.Equal((1, 300m, 600m), (andamento.ParcelasPagas, andamento.ValorPago, andamento.ValorQueFalta));
    }

    [Fact]
    public void R38_DividaDaCasa_EPermitida()
    {
        var financiamento = Cartao() with { DonoId = null };

        Assert.True(Divida.Lancar(_casal.Familia, _casal.Bruno.Id, financiamento with { DonoId = null }).Sucesso);
    }

    [Fact]
    public void R37_AdultoNaoLancaDividaNoNomeDoOutro()
    {
        Assert.Equal("R37", Divida.Lancar(_casal.Familia, _casal.Ana.Id, Cartao(donoId: _casal.Bruno.Id)).CodigoDaRegra);
    }

    [Fact]
    public void R37_ParcelaDaDividaDoOutro_SoODonoMarcaComoPaga()
    {
        var doBruno = Divida.Lancar(_casal.Familia, _casal.Bruno.Id, Cartao(donoId: _casal.Bruno.Id)).Valor;

        var pelaAna = doBruno.Parcelas[0].MarcarComoPaga(_casal.Familia, _casal.Ana.Id, _casal.Ana.Id, Casal.Dia(10));

        Assert.Equal("R37", pelaAna.CodigoDaRegra);
    }

    [Fact]
    public void R4_CriancaNaoLancaDivida()
    {
        Assert.Equal("R4", Divida.Lancar(_casal.Familia, _casal.Lia.Id, Cartao() with { DonoId = null }).CodigoDaRegra);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(121)]
    public void NumeroDeParcelasForaDoLimite_EDadoInvalido(int parcelas)
    {
        Assert.Throws<DadosInvalidosException>(() => Divida.Lancar(_casal.Familia, _casal.Ana.Id, Cartao(parcelas: parcelas)));
    }
}
