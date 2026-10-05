using System.Net;
using System.Net.Http.Json;
using RegraDeCasamento.Shared.Comum;
using RegraDeCasamento.Shared.Financas;

namespace RegraDeCasamento.Web.Tests.Financas;

public sealed class FinancasTests(AplicacaoDeTeste aplicacao) : IClassFixture<AplicacaoDeTeste>
{
    private static readonly DateOnly DiaCinco = new(2026, 10, 5);

    [Fact]
    public async Task R8_R10_ContaLancadaEPagaAparecePagaComQuemPagou()
    {
        var familia = await aplicacao.FamiliaCompletaAsync();

        var aluguel = await LancarAsync(familia.Ana, "Aluguel", 1500m, null);
        var paga = await familia.Ana.PostAsJsonAsync($"/api/financas/despesas/{aluguel.Id}/pagar", new DadosDoPagamento(familia.BrunoId, DiaCinco));
        var doMes = await familia.Bruno.GetFromJsonAsync<List<DespesaDto>>("/api/financas/despesas?ano=2026&mes=10");

        Assert.Equal(HttpStatusCode.OK, paga.StatusCode);
        var conta = Assert.Single(doMes!);
        Assert.Equal(SituacaoDeConta.Paga, conta.Situacao);
        Assert.Equal("Bruno", conta.PagaPor);
        Assert.Equal("Casa", conta.Dono);
    }

    [Fact]
    public async Task R10_ContaPagaDuasVezes_Recebe422()
    {
        var familia = await aplicacao.FamiliaCompletaAsync();
        var luz = await LancarAsync(familia.Ana, "Luz", 200m, null);
        await familia.Ana.PostAsJsonAsync($"/api/financas/despesas/{luz.Id}/pagar", new DadosDoPagamento(familia.AnaId, DiaCinco));

        var deNovo = await familia.Ana.PostAsJsonAsync($"/api/financas/despesas/{luz.Id}/pagar", new DadosDoPagamento(familia.BrunoId, DiaCinco));

        await AssertRegraAsync(deNovo, "R10");
    }

    [Fact]
    public async Task R9_ContribuicaoDoMes()
    {
        var familia = await aplicacao.FamiliaCompletaAsync();
        var aluguel = await LancarAsync(familia.Ana, "Aluguel", 1500m, null);
        var internet = await LancarAsync(familia.Ana, "Internet", 500m, null);
        var pensao = await LancarAsync(familia.Bruno, "Pensão paga", 900m, familia.BrunoId);
        await familia.Ana.PostAsJsonAsync($"/api/financas/despesas/{aluguel.Id}/pagar", new DadosDoPagamento(familia.AnaId, DiaCinco));
        await familia.Ana.PostAsJsonAsync($"/api/financas/despesas/{internet.Id}/pagar", new DadosDoPagamento(familia.BrunoId, DiaCinco));
        await familia.Bruno.PostAsJsonAsync($"/api/financas/despesas/{pensao.Id}/pagar", new DadosDoPagamento(familia.BrunoId, DiaCinco));

        var contribuicao = await familia.Bruno.GetFromJsonAsync<ContribuicaoDoMesDto>("/api/financas/contribuicao?ano=2026&mes=10");

        Assert.Equal(2000m, contribuicao!.TotalDaCasa);
        Assert.Equal(75.0m, contribuicao.Adultos.Single(a => a.MembroId == familia.AnaId).Porcentagem);
        Assert.Equal(25.0m, contribuicao.Adultos.Single(a => a.MembroId == familia.BrunoId).Porcentagem);
    }

    [Fact]
    public async Task R9_PlanilhaDaContribuicao_TemOsMesmosValoresDaTela()
    {
        var familia = await aplicacao.FamiliaCompletaAsync();
        var aluguel = await LancarAsync(familia.Ana, "Aluguel", 1500m, null);
        var luz = await LancarAsync(familia.Ana, "Luz", 500m, null);
        await familia.Ana.PostAsJsonAsync($"/api/financas/despesas/{aluguel.Id}/pagar", new DadosDoPagamento(familia.AnaId, DiaCinco));
        await familia.Ana.PostAsJsonAsync($"/api/financas/despesas/{luz.Id}/pagar", new DadosDoPagamento(familia.BrunoId, DiaCinco));

        var resposta = await familia.Bruno.GetAsync("/api/financas/contribuicao/planilha?ano=2026&mes=10");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("contribuicao-2026-10.xlsx", resposta.Content.Headers.ContentDisposition?.FileNameStar ?? resposta.Content.Headers.ContentDisposition?.FileName);

        using var pasta = new ClosedXML.Excel.XLWorkbook(await resposta.Content.ReadAsStreamAsync());
        var resumo = pasta.Worksheet("Contribuição");
        Assert.Equal("Ana", resumo.Cell(5, 1).GetString());
        Assert.Equal(1500m, resumo.Cell(5, 2).GetValue<decimal>());
        Assert.Equal(0.75m, resumo.Cell(5, 3).GetValue<decimal>());
        Assert.Equal("Bruno", resumo.Cell(6, 1).GetString());
        Assert.Equal(2000m, resumo.Cell(7, 2).GetValue<decimal>());
        Assert.Equal(2, pasta.Worksheet("Contas da casa pagas").RangeUsed()!.RowCount() - 1);
    }

    [Fact]
    public async Task R4_CriancaNaoBaixaAPlanilha()
    {
        var familia = await aplicacao.FamiliaCompletaAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await familia.Lia.GetAsync("/api/financas/contribuicao/planilha")).StatusCode);
    }

    [Fact]
    public async Task R7_RendaTemporariaSemDataDeFim_Recebe422()
    {
        var familia = await aplicacao.FamiliaCompletaAsync();

        var resposta = await familia.Ana.PostAsJsonAsync("/api/financas/rendas",
            new DadosDaNovaRenda("Bico", 800m, familia.AnaId, TipoDeRendaDto.Temporaria, 10, false, null, null));

        await AssertRegraAsync(resposta, "R7");
    }

    [Fact]
    public async Task R7_RendaLancadaApareceNaLista()
    {
        var familia = await aplicacao.FamiliaCompletaAsync();

        var resposta = await familia.Ana.PostAsJsonAsync("/api/financas/rendas",
            new DadosDaNovaRenda("Salário", 4000m, familia.AnaId, TipoDeRendaDto.Fixa, 5, true, null, null));
        var rendas = await familia.Bruno.GetFromJsonAsync<List<RendaDto>>("/api/financas/rendas");

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var salario = Assert.Single(rendas!);
        Assert.Equal(("Ana", TipoDeRendaDto.Fixa, 5, true), (salario.Dono, salario.Tipo, salario.DiaDeRecebimento, salario.EmDiaUtil));
    }

    [Fact]
    public async Task R6_CriancaComoDonaDaDespesa_Recebe422()
    {
        var familia = await aplicacao.FamiliaCompletaAsync();

        var resposta = await familia.Ana.PostAsJsonAsync("/api/financas/despesas", new DadosDaNovaDespesa("Escola", 800m, DiaCinco, familia.LiaId));

        await AssertRegraAsync(resposta, "R6");
    }

    [Theory]
    [InlineData("/api/financas/despesas")]
    [InlineData("/api/financas/rendas")]
    [InlineData("/api/financas/contribuicao")]
    public async Task R4_CriancaNaoVeFinancas(string endereco)
    {
        var familia = await aplicacao.FamiliaCompletaAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await familia.Lia.GetAsync(endereco)).StatusCode);
    }

    [Fact]
    public async Task R4_CriancaNaoLancaDespesa()
    {
        var familia = await aplicacao.FamiliaCompletaAsync();

        var resposta = await familia.Lia.PostAsJsonAsync("/api/financas/despesas", new DadosDaNovaDespesa("Sorvete", 10m, DiaCinco, null));

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task R5_FamiliaNaoVeNemPagaContaDeOutraFamilia()
    {
        var silva = await aplicacao.FamiliaCompletaAsync();
        var souza = await aplicacao.FamiliaCompletaAsync();
        var aluguelDosSilva = await LancarAsync(silva.Ana, "Aluguel dos Silva", 1500m, null);

        var listaDosSouza = await souza.Ana.GetFromJsonAsync<List<DespesaDto>>("/api/financas/despesas?ano=2026&mes=10");
        var pagar = await souza.Ana.PostAsJsonAsync($"/api/financas/despesas/{aluguelDosSilva.Id}/pagar", new DadosDoPagamento(souza.AnaId, DiaCinco));

        Assert.Empty(listaDosSouza!);
        Assert.Equal(HttpStatusCode.NotFound, pagar.StatusCode);
    }

    [Fact]
    public async Task ValorNegativo_Recebe400()
    {
        var familia = await aplicacao.FamiliaCompletaAsync();

        var resposta = await familia.Ana.PostAsJsonAsync("/api/financas/despesas", new DadosDaNovaDespesa("Luz", -5m, DiaCinco, null));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task MesInvalido_Recebe400()
    {
        var familia = await aplicacao.FamiliaCompletaAsync();

        Assert.Equal(HttpStatusCode.BadRequest, (await familia.Ana.GetAsync("/api/financas/despesas?ano=2026&mes=13")).StatusCode);
    }

    private static async Task<DespesaDto> LancarAsync(HttpClient adulto, string descricao, decimal valor, Guid? donoId)
    {
        var resposta = await adulto.PostAsJsonAsync("/api/financas/despesas", new DadosDaNovaDespesa(descricao, valor, DiaCinco, donoId));
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<DespesaDto>())!;
    }

    private static async Task AssertRegraAsync(HttpResponseMessage resposta, string codigo)
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        Assert.Equal(codigo, (await resposta.Content.ReadFromJsonAsync<FalhaDeRegraDto>())!.CodigoDaRegra);
    }
}
