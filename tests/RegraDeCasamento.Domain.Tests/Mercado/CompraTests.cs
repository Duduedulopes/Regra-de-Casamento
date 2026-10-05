using RegraDeCasamento.Domain.Comum;
using RegraDeCasamento.Domain.Familias;
using RegraDeCasamento.Domain.Mercado;
using RegraDeCasamento.Domain.Tests.Financas;

namespace RegraDeCasamento.Domain.Tests.Mercado;

public class CompraTests
{
    private readonly Casal _casal = new();

    private NovaCompra DoMes(FinalidadeDaCompra finalidade = FinalidadeDaCompra.SuprimentoDaCasa, Guid? paraQuem = null, params NovoItem[] itens) =>
        new(Casal.Dia(4), "Atacadão", finalidade, paraQuem,
            itens.Length > 0 ? itens : [new NovoItem("Arroz 5 kg", 2, 25.90m), new NovoItem("Banana", 1.5m, 6m)]);

    [Fact]
    public void R12_CompraGuardaOQueQuemEParaQue()
    {
        var compra = Compra.Lancar(_casal.Familia, _casal.Ana.Id, DoMes()).Valor;

        Assert.Equal(_casal.Ana.Id, compra.CompradoPorId);
        Assert.Equal(FinalidadeDaCompra.SuprimentoDaCasa, compra.Finalidade);
        Assert.Equal(["Arroz 5 kg", "Banana"], compra.Itens.Select(i => i.Descricao));
        Assert.Equal(60.80m, compra.Total);
    }

    [Fact]
    public void R12_CompraSemItens_NaoPode()
    {
        var semItens = DoMes() with { Itens = [] };

        Assert.Equal("R12", Compra.Lancar(_casal.Familia, _casal.Ana.Id, semItens).CodigoDaRegra);
    }

    [Fact]
    public void R37_QuemLancaACompra_EQuemComprou()
    {
        var doBruno = Compra.Lancar(_casal.Familia, _casal.Bruno.Id, DoMes()).Valor;

        Assert.Equal(_casal.Bruno.Id, doBruno.CompradoPorId);
    }

    [Fact]
    public void R12_CompraPessoal_PrecisaDizerParaQuem()
    {
        Assert.Equal("R12", Compra.Lancar(_casal.Familia, _casal.Ana.Id, DoMes(FinalidadeDaCompra.Pessoal)).CodigoDaRegra);
    }

    [Fact]
    public void R12_CompraPessoal_PodeSerParaUmaCrianca()
    {
        var compra = Compra.Lancar(_casal.Familia, _casal.Ana.Id, DoMes(FinalidadeDaCompra.Pessoal, _casal.Lia.Id)).Valor;

        Assert.Equal(_casal.Lia.Id, compra.ParaQuemId);
    }

    [Fact]
    public void R12_ParaQuemDeOutraFamilia_NaoPode()
    {
        var deFora = Familia.Criar("Família Souza", "Paula").Membros.Single();

        Assert.Equal("R12", Compra.Lancar(_casal.Familia, _casal.Ana.Id, DoMes(FinalidadeDaCompra.Pessoal, deFora.Id)).CodigoDaRegra);
    }

    [Fact]
    public void R12_ParaQuemSoExisteNaCompraPessoal()
    {
        var compra = Compra.Lancar(_casal.Familia, _casal.Ana.Id, DoMes(FinalidadeDaCompra.LancheParaTodos, _casal.Lia.Id)).Valor;

        Assert.Null(compra.ParaQuemId);
    }

    [Fact]
    public void R4_CriancaNaoLancaCompra()
    {
        Assert.Equal("R4", Compra.Lancar(_casal.Familia, _casal.Lia.Id, DoMes()).CodigoDaRegra);
    }

    [Fact]
    public void R5_AdultoDeOutraFamilia_NaoLancaCompraAqui()
    {
        var paula = Familia.Criar("Família Souza", "Paula").Membros.Single();

        Assert.Equal("R5", Compra.Lancar(_casal.Familia, paula.Id, DoMes()).CodigoDaRegra);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(-1, 10)]
    public void ItemComQuantidadeOuValorZero_EDadoInvalido(decimal quantidade, decimal valor)
    {
        var compra = DoMes(itens: new NovoItem("Leite", quantidade, valor));

        Assert.Throws<DadosInvalidosException>(() => Compra.Lancar(_casal.Familia, _casal.Ana.Id, compra));
    }

    [Fact]
    public void ItensTemIdGeradoNoCSharp()
    {
        var compra = Compra.Lancar(_casal.Familia, _casal.Ana.Id, DoMes()).Valor;

        Assert.All(compra.Itens, item => Assert.NotEqual(Guid.Empty, item.Id));
    }
}
