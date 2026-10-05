using RegraDeCasamento.Domain.Comum;
using RegraDeCasamento.Domain.Financas;

namespace RegraDeCasamento.Domain.Tests.Financas;

public class RendaTests
{
    private readonly Casal _casal = new();

    private static NovaRenda Salario(Guid? donoId) =>
        new("Salário", 4000m, donoId, TipoDeRenda.Fixa, DiaDeRecebimento: 5, EmDiaUtil: true, DataDeFim: null, UsoRestrito: null);

    [Fact]
    public void R7_SalarioNoQuintoDiaUtil()
    {
        var renda = Renda.Lancar(_casal.Familia, _casal.Ana.Id, Salario(_casal.Ana.Id)).Valor;

        Assert.Equal(TipoDeRenda.Fixa, renda.Tipo);
        Assert.Equal(5, renda.DiaDeRecebimento);
        Assert.True(renda.EmDiaUtil);
        Assert.Equal(_casal.Ana.Id, renda.DonoId);
    }

    [Fact]
    public void R37_AdultoNaoLancaRendaNoNomeDoOutro()
    {
        Assert.Equal("R37", Renda.Lancar(_casal.Familia, _casal.Ana.Id, Salario(_casal.Bruno.Id)).CodigoDaRegra);
    }

    [Fact]
    public void R7_RendaTemporaria_PrecisaDaDataDeFim()
    {
        var semFim = Salario(_casal.Ana.Id) with { Tipo = TipoDeRenda.Temporaria };

        var resultado = Renda.Lancar(_casal.Familia, _casal.Ana.Id, semFim);

        Assert.Equal("R7", resultado.CodigoDaRegra);
    }

    [Fact]
    public void R7_RendaTemporaria_ValeAteADataDeFim()
    {
        var porQuatroMeses = Salario(_casal.Ana.Id) with { Tipo = TipoDeRenda.Temporaria, DataDeFim = Casal.Dia(31, mes: 1, ano: 2027) };

        var renda = Renda.Lancar(_casal.Familia, _casal.Ana.Id, porQuatroMeses).Valor;

        Assert.True(renda.ValeEm(Casal.Dia(31, mes: 1, ano: 2027)));
        Assert.False(renda.ValeEm(Casal.Dia(1, mes: 2, ano: 2027)));
    }

    [Fact]
    public void R7_BeneficioDeUsoRestrito_PrecisaDizerOUso()
    {
        var vale = Salario(_casal.Ana.Id) with { Tipo = TipoDeRenda.BeneficioDeUsoRestrito, UsoRestrito = " " };

        var resultado = Renda.Lancar(_casal.Familia, _casal.Ana.Id, vale);

        Assert.Equal("R7", resultado.CodigoDaRegra);
    }

    [Fact]
    public void R7_BeneficioDeUsoRestrito_GuardaOUso()
    {
        var vale = Salario(_casal.Ana.Id) with { Tipo = TipoDeRenda.BeneficioDeUsoRestrito, UsoRestrito = "comida" };

        var renda = Renda.Lancar(_casal.Familia, _casal.Ana.Id, vale).Valor;

        Assert.Equal("comida", renda.UsoRestrito);
    }

    [Fact]
    public void R7_DataDeFimSoExisteNaRendaTemporaria()
    {
        var fixaComFim = Salario(_casal.Ana.Id) with { DataDeFim = Casal.Dia(1) };

        var renda = Renda.Lancar(_casal.Familia, _casal.Ana.Id, fixaComFim).Valor;

        Assert.Null(renda.DataDeFim);
        Assert.True(renda.ValeEm(Casal.Dia(1, ano: 2030)));
    }

    [Fact]
    public void R6_RendaDaCasa_EPermitida()
    {
        var aluguelRecebido = Salario(donoId: null) with { Descricao = "Aluguel do apartamento" };

        Assert.True(Renda.Lancar(_casal.Familia, _casal.Ana.Id, aluguelRecebido).Sucesso);
    }

    [Fact]
    public void R6_CriancaNaoEDonaDeRenda()
    {
        Assert.Equal("R6", Renda.Lancar(_casal.Familia, _casal.Ana.Id, Salario(_casal.Lia.Id)).CodigoDaRegra);
    }

    [Fact]
    public void R4_CriancaNaoLancaRenda()
    {
        Assert.Equal("R4", Renda.Lancar(_casal.Familia, _casal.Lia.Id, Salario(null)).CodigoDaRegra);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void DiaForaDoMes_EDadoInvalido(int dia)
    {
        var renda = Salario(_casal.Ana.Id) with { DiaDeRecebimento = dia };

        Assert.Throws<DadosInvalidosException>(() => Renda.Lancar(_casal.Familia, _casal.Ana.Id, renda));
    }
}
