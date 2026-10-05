using RegraDeCasamento.Domain.Financas;

namespace RegraDeCasamento.Domain.Tests.Financas;

public class ContribuicaoDoMesTests
{
    private readonly Casal _casal = new();
    private readonly List<Despesa> _despesas = [];

    private Despesa Paga(string descricao, decimal valor, Guid pagaPorId, DateOnly pagaEm, Guid? donoId = null)
    {
        // Quem lança e marca é o dono da despesa (R37); a da casa, qualquer adulto.
        var quemLanca = donoId ?? _casal.Ana.Id;
        var despesa = Despesa.Lancar(_casal.Familia, quemLanca, descricao, valor, pagaEm, donoId).Valor;
        despesa.MarcarComoPaga(_casal.Familia, quemLanca, pagaPorId, pagaEm);
        _despesas.Add(despesa);
        return despesa;
    }

    [Fact]
    public void R9_MostraQuantoCadaAdultoPagouDasContasDaCasa()
    {
        Paga("Aluguel", 1500m, _casal.Ana.Id, Casal.Dia(5));
        Paga("Luz", 250m, _casal.Bruno.Id, Casal.Dia(12));
        Paga("Internet", 250m, _casal.Bruno.Id, Casal.Dia(15));

        var contribuicao = ContribuicaoDoMes.Calcular(_casal.Familia, _despesas, 2026, 10);

        Assert.Equal(2000m, contribuicao.TotalDaCasa);
        var ana = contribuicao.Adultos.Single(a => a.MembroId == _casal.Ana.Id);
        var bruno = contribuicao.Adultos.Single(a => a.MembroId == _casal.Bruno.Id);
        Assert.Equal((1500m, 75.0m), (ana.Valor, ana.Porcentagem));
        Assert.Equal((500m, 25.0m), (bruno.Valor, bruno.Porcentagem));
    }

    [Fact]
    public void R9_DespesaDeUmAdulto_NaoEntraNaContribuicao()
    {
        Paga("Aluguel", 1000m, _casal.Ana.Id, Casal.Dia(5));
        Paga("Pensão paga", 900m, _casal.Bruno.Id, Casal.Dia(5), donoId: _casal.Bruno.Id);

        var contribuicao = ContribuicaoDoMes.Calcular(_casal.Familia, _despesas, 2026, 10);

        Assert.Equal(1000m, contribuicao.TotalDaCasa);
        Assert.Equal(100.0m, contribuicao.Adultos.Single(a => a.MembroId == _casal.Ana.Id).Porcentagem);
        Assert.Equal(0m, contribuicao.Adultos.Single(a => a.MembroId == _casal.Bruno.Id).Valor);
    }

    [Fact]
    public void R9_ContaAPagar_NaoEntra()
    {
        _despesas.Add(Despesa.Lancar(_casal.Familia, _casal.Ana.Id, "Água", 80m, Casal.Dia(20), null).Valor);

        var contribuicao = ContribuicaoDoMes.Calcular(_casal.Familia, _despesas, 2026, 10);

        Assert.Equal(0m, contribuicao.TotalDaCasa);
        Assert.All(contribuicao.Adultos, adulto => Assert.Equal(0m, adulto.Porcentagem));
    }

    [Fact]
    public void R9_ContaOMesDoPagamento_NaoODoVencimento()
    {
        var setembro = Despesa.Lancar(_casal.Familia, _casal.Ana.Id, "Aluguel de setembro", 1500m, Casal.Dia(30, mes: 9), null).Valor;
        setembro.MarcarComoPaga(_casal.Familia, _casal.Ana.Id, _casal.Bruno.Id, Casal.Dia(2));
        _despesas.Add(setembro);

        Assert.Equal(1500m, ContribuicaoDoMes.Calcular(_casal.Familia, _despesas, 2026, 10).TotalDaCasa);
        Assert.Equal(0m, ContribuicaoDoMes.Calcular(_casal.Familia, _despesas, 2026, 9).TotalDaCasa);
    }

    [Fact]
    public void R9_SoOsAdultosAparecem()
    {
        var contribuicao = ContribuicaoDoMes.Calcular(_casal.Familia, _despesas, 2026, 10);

        Assert.Equal([_casal.Ana.Id, _casal.Bruno.Id], contribuicao.Adultos.Select(a => a.MembroId));
    }

    [Fact]
    public void R5_DespesaDeOutraFamilia_NaoEntra()
    {
        var outro = new Casal();
        var deOutraFamilia = Despesa.Lancar(outro.Familia, outro.Ana.Id, "Luz", 300m, Casal.Dia(5), null).Valor;
        deOutraFamilia.MarcarComoPaga(outro.Familia, outro.Ana.Id, outro.Ana.Id, Casal.Dia(5));
        _despesas.Add(deOutraFamilia);

        Assert.Equal(0m, ContribuicaoDoMes.Calcular(_casal.Familia, _despesas, 2026, 10).TotalDaCasa);
    }
}
