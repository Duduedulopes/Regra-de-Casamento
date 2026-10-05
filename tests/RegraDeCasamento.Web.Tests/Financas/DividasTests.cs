using System.Net;
using System.Net.Http.Json;
using RegraDeCasamento.Shared.Comum;
using RegraDeCasamento.Shared.Financas;

namespace RegraDeCasamento.Web.Tests.Financas;

public sealed class DividasTests(AplicacaoDeTeste aplicacao) : IClassFixture<AplicacaoDeTeste>
{
    private static readonly DateOnly DiaDez = new(2026, 10, 10);

    [Fact]
    public async Task R38_DividaLancada_ParcelasViramContasEAndamentoAparece()
    {
        var familia = await aplicacao.FamiliaCompletaAsync();

        var resposta = await familia.Bruno.PostAsJsonAsync("/api/financas/dividas",
            new DadosDaNovaDivida("Geladeira", TipoDeDividaDto.CompraParcelada, 900m, 3, DiaDez, familia.BrunoId));
        var contasDeNovembro = await familia.Ana.GetFromJsonAsync<List<DespesaDto>>("/api/financas/despesas?ano=2026&mes=11");
        var parcela = Assert.Single(contasDeNovembro!);
        await familia.Bruno.PostAsJsonAsync($"/api/financas/despesas/{parcela.Id}/pagar", new DadosDoPagamento(familia.BrunoId, DiaDez.AddMonths(1)));
        var dividas = await familia.Ana.GetFromJsonAsync<List<DividaDto>>("/api/financas/dividas");

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.Equal("Geladeira (2/3)", parcela.Descricao);
        Assert.Equal(300m, parcela.Valor);
        var divida = Assert.Single(dividas!);
        Assert.Equal(("Bruno", 1, 300m, 600m), (divida.Dono, divida.ParcelasPagas, divida.ValorPago, divida.ValorQueFalta));
    }

    [Fact]
    public async Task R37_AdultoNaoLancaDividaNoNomeDoOutro()
    {
        var familia = await aplicacao.FamiliaCompletaAsync();

        var resposta = await familia.Ana.PostAsJsonAsync("/api/financas/dividas",
            new DadosDaNovaDivida("Cartão", TipoDeDividaDto.CartaoDeCredito, 500m, 1, DiaDez, familia.BrunoId));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        Assert.Equal("R37", (await resposta.Content.ReadFromJsonAsync<FalhaDeRegraDto>())!.CodigoDaRegra);
    }

    [Fact]
    public async Task R37_OutroAdultoVeMasNaoPagaAContaDoDono()
    {
        var familia = await aplicacao.FamiliaCompletaAsync();
        var lancada = await familia.Bruno.PostAsJsonAsync("/api/financas/despesas", new DadosDaNovaDespesa("Pensão paga", 900m, DiaDez, familia.BrunoId));
        var pensao = (await lancada.Content.ReadFromJsonAsync<DespesaDto>())!;

        var vistaPelaAna = await familia.Ana.GetFromJsonAsync<List<DespesaDto>>("/api/financas/despesas?ano=2026&mes=10");
        var pagaPelaAna = await familia.Ana.PostAsJsonAsync($"/api/financas/despesas/{pensao.Id}/pagar", new DadosDoPagamento(familia.AnaId, DiaDez));

        Assert.Contains(vistaPelaAna!, d => d.Id == pensao.Id);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, pagaPelaAna.StatusCode);
        Assert.Equal("R37", (await pagaPelaAna.Content.ReadFromJsonAsync<FalhaDeRegraDto>())!.CodigoDaRegra);
    }

    [Fact]
    public async Task R4_CriancaNaoVeDividas()
    {
        var familia = await aplicacao.FamiliaCompletaAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await familia.Lia.GetAsync("/api/financas/dividas")).StatusCode);
    }
}
