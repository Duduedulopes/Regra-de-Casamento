using RegraDeCasamento.Domain.Mercado;
using RegraDeCasamento.Domain.Tests.Financas;

namespace RegraDeCasamento.Domain.Tests.Mercado;

public class ResumoDoMercadoTests
{
    private readonly Casal _casal = new();
    private readonly List<Compra> _compras = [];

    private void Comprar(Guid quem, FinalidadeDaCompra finalidade, decimal valor, DateOnly dia, Guid? paraQuem = null) =>
        _compras.Add(Compra.Lancar(_casal.Familia, quem,
            new NovaCompra(dia, "Mercado", finalidade, paraQuem, [new NovoItem("Item", 1, valor)])).Valor);

    [Fact]
    public void R13_TotalDoMesPorFinalidadeEPorQuemComprou()
    {
        Comprar(_casal.Ana.Id, FinalidadeDaCompra.SuprimentoDaCasa, 300m, Casal.Dia(3));
        Comprar(_casal.Bruno.Id, FinalidadeDaCompra.SuprimentoDaCasa, 200m, Casal.Dia(10));
        Comprar(_casal.Bruno.Id, FinalidadeDaCompra.LancheParaTodos, 80m, Casal.Dia(11));
        Comprar(_casal.Ana.Id, FinalidadeDaCompra.Pessoal, 50m, Casal.Dia(12), _casal.Lia.Id);

        var resumo = ResumoDoMercado.Calcular(_casal.Familia, _compras, 2026, 10);

        Assert.Equal(630m, resumo.Total);
        Assert.Equal(500m, resumo.PorFinalidade.Single(f => f.Finalidade == FinalidadeDaCompra.SuprimentoDaCasa).Total);
        Assert.Equal(80m, resumo.PorFinalidade.Single(f => f.Finalidade == FinalidadeDaCompra.LancheParaTodos).Total);
        Assert.Equal(50m, resumo.PorFinalidade.Single(f => f.Finalidade == FinalidadeDaCompra.Pessoal).Total);
        Assert.Equal((350m, 2), resumo.PorComprador.Where(c => c.MembroId == _casal.Ana.Id).Select(c => (c.Total, c.Compras)).Single());
        Assert.Equal((280m, 2), resumo.PorComprador.Where(c => c.MembroId == _casal.Bruno.Id).Select(c => (c.Total, c.Compras)).Single());
    }

    [Fact]
    public void R13_SoEntraOMesPedido()
    {
        Comprar(_casal.Ana.Id, FinalidadeDaCompra.SuprimentoDaCasa, 300m, Casal.Dia(30, mes: 9));
        Comprar(_casal.Ana.Id, FinalidadeDaCompra.SuprimentoDaCasa, 100m, Casal.Dia(1));

        Assert.Equal(100m, ResumoDoMercado.Calcular(_casal.Familia, _compras, 2026, 10).Total);
    }

    [Fact]
    public void R13_MesSemCompras_MostraTudoZerado()
    {
        var resumo = ResumoDoMercado.Calcular(_casal.Familia, _compras, 2026, 10);

        Assert.Equal(0m, resumo.Total);
        Assert.Equal(3, resumo.PorFinalidade.Count);
        Assert.Equal(2, resumo.PorComprador.Count);
    }
}
