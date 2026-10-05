using System.Net;
using System.Net.Http.Json;
using RegraDeCasamento.Shared.Comum;
using RegraDeCasamento.Shared.Mercado;

namespace RegraDeCasamento.Web.Tests.Mercado;

public sealed class MercadoTests(AplicacaoDeTeste aplicacao) : IClassFixture<AplicacaoDeTeste>
{
    private static readonly DateOnly DiaQuatro = new(2026, 10, 4);

    private static DadosDaNovaCompra Feira(FinalidadeDaCompraDto finalidade = FinalidadeDaCompraDto.SuprimentoDaCasa, Guid? paraQuem = null) =>
        new(DiaQuatro, "Atacadão", finalidade, paraQuem, [new ItemDaNovaCompra("Arroz 5 kg", 2, 25.90m), new ItemDaNovaCompra("Banana", 1.5m, 6m)]);

    [Fact]
    public async Task R12_CompraLancada_GuardaItensQuemComprouEParaQue()
    {
        var familia = await aplicacao.FamiliaCompletaAsync();

        var resposta = await familia.Bruno.PostAsJsonAsync("/api/mercado/compras", Feira());
        var doMes = await familia.Ana.GetFromJsonAsync<List<CompraDto>>("/api/mercado/compras?ano=2026&mes=10");

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var compra = Assert.Single(doMes!);
        Assert.Equal(("Bruno", FinalidadeDaCompraDto.SuprimentoDaCasa, 60.80m), (compra.CompradoPor, compra.Finalidade, compra.Total));
        Assert.Equal(2, compra.Itens.Count);
    }

    [Fact]
    public async Task R12_CompraPessoalSemParaQuem_Recebe422()
    {
        var familia = await aplicacao.FamiliaCompletaAsync();

        var resposta = await familia.Ana.PostAsJsonAsync("/api/mercado/compras", Feira(FinalidadeDaCompraDto.Pessoal));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        Assert.Equal("R12", (await resposta.Content.ReadFromJsonAsync<FalhaDeRegraDto>())!.CodigoDaRegra);
    }

    [Fact]
    public async Task R13_ResumoEPlanilhaDoMes()
    {
        var familia = await aplicacao.FamiliaCompletaAsync();
        await familia.Ana.PostAsJsonAsync("/api/mercado/compras", Feira());
        await familia.Bruno.PostAsJsonAsync("/api/mercado/compras", Feira(FinalidadeDaCompraDto.Pessoal, familia.LiaId));

        var resumo = await familia.Ana.GetFromJsonAsync<ResumoDoMercadoDto>("/api/mercado/resumo?ano=2026&mes=10");
        var planilha = await familia.Ana.GetAsync("/api/mercado/planilha?ano=2026&mes=10");

        Assert.Equal(121.60m, resumo!.Total);
        Assert.Equal(60.80m, resumo.PorFinalidade.Single(f => f.Finalidade == FinalidadeDaCompraDto.Pessoal).Total);

        Assert.Equal(HttpStatusCode.OK, planilha.StatusCode);
        using var pasta = new ClosedXML.Excel.XLWorkbook(await planilha.Content.ReadAsStreamAsync());
        var itens = pasta.Worksheet("Compras");
        Assert.Equal(4, itens.RangeUsed()!.RowCount() - 1);
        Assert.Equal("Lia", itens.Cell(4, 5).GetString());
        Assert.Equal(121.60m, pasta.Worksheet("Resumo").Cell(7, 3).GetValue<decimal>());
    }

    [Theory]
    [InlineData("/api/mercado/compras")]
    [InlineData("/api/mercado/resumo")]
    [InlineData("/api/mercado/planilha")]
    public async Task R4_CriancaNaoVeOMercado(string endereco)
    {
        var familia = await aplicacao.FamiliaCompletaAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await familia.Lia.GetAsync(endereco)).StatusCode);
    }

    [Fact]
    public async Task R5_FamiliaNaoVeAsComprasDaOutra()
    {
        var silva = await aplicacao.FamiliaCompletaAsync();
        var souza = await aplicacao.FamiliaCompletaAsync();
        await silva.Ana.PostAsJsonAsync("/api/mercado/compras", Feira());

        Assert.Empty((await souza.Ana.GetFromJsonAsync<List<CompraDto>>("/api/mercado/compras?ano=2026&mes=10"))!);
    }
}
