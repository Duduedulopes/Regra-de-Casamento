using Microsoft.AspNetCore.Http.HttpResults;
using RegraDeCasamento.Application.Financas;
using RegraDeCasamento.Application.Relatorios;
using RegraDeCasamento.Domain.Comum;
using RegraDeCasamento.Shared.Acesso;
using RegraDeCasamento.Shared.Comum;
using RegraDeCasamento.Shared.Financas;

namespace RegraDeCasamento.Web.Financas;

/// <summary>Contas, rendas e contribuição do mês. Tudo só para adultos (R4); a policy é conferida aqui, no servidor.</summary>
internal static class EndpointsDeFinancas
{
    private static readonly TimeZoneInfo HorarioDeBrasilia = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public static RouteGroupBuilder MapEndpointsDeFinancas(this RouteGroupBuilder api)
    {
        var financas = api.MapGroup("/financas").RequireAuthorization(Politicas.Adulto);
        financas.MapGet("/despesas", ListarDespesasAsync);
        financas.MapPost("/despesas", LancarDespesaAsync);
        financas.MapPost("/despesas/{despesaId:guid}/pagar", PagarAsync);
        financas.MapPost("/despesas/{despesaId:guid}/desfazer-pagamento", DesfazerPagamentoAsync);
        financas.MapGet("/rendas", ListarRendasAsync);
        financas.MapPost("/rendas", LancarRendaAsync);
        financas.MapGet("/dividas", ListarDividasAsync);
        financas.MapPost("/dividas", LancarDividaAsync);
        financas.MapGet("/contribuicao", ContribuicaoAsync);
        financas.MapGet("/contribuicao/planilha", PlanilhaDaContribuicaoAsync);
        return api;
    }

    private static async Task<FileContentHttpResult> PlanilhaDaContribuicaoAsync(
        int? ano, int? mes, FinancasDaCasa financas, IGeradorDePlanilhas planilhas, CancellationToken cancellationToken)
    {
        var (anoDoFiltro, mesDoFiltro) = MesPedido(ano, mes);
        var arquivo = await financas.PlanilhaDaContribuicaoAsync(anoDoFiltro, mesDoFiltro, planilhas, cancellationToken);
        return TypedResults.File(
            arquivo,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"contribuicao-{anoDoFiltro}-{mesDoFiltro:00}.xlsx");
    }

    private static async Task<Ok<IReadOnlyList<DespesaDto>>> ListarDespesasAsync(
        int? ano, int? mes, FinancasDaCasa financas, CancellationToken cancellationToken)
    {
        var (anoDoFiltro, mesDoFiltro) = MesPedido(ano, mes);
        return TypedResults.Ok(await financas.ListarDespesasAsync(anoDoFiltro, mesDoFiltro, cancellationToken));
    }

    private static async Task<Results<Created<DespesaDto>, UnprocessableEntity<FalhaDeRegraDto>>> LancarDespesaAsync(
        DadosDaNovaDespesa dados, FinancasDaCasa financas, CancellationToken cancellationToken)
    {
        var resultado = await financas.LancarDespesaAsync(dados, cancellationToken);
        return resultado.Falhou
            ? RespostasDaApi.FalhaDeRegra(resultado)
            : TypedResults.Created($"/api/financas/despesas/{resultado.Valor.Id}", resultado.Valor);
    }

    private static async Task<Results<Ok<DespesaDto>, NotFound, UnprocessableEntity<FalhaDeRegraDto>>> PagarAsync(
        Guid despesaId, DadosDoPagamento dados, FinancasDaCasa financas, CancellationToken cancellationToken) =>
        Responder(await financas.MarcarComoPagaAsync(despesaId, dados, cancellationToken));

    private static async Task<Results<Ok<DespesaDto>, NotFound, UnprocessableEntity<FalhaDeRegraDto>>> DesfazerPagamentoAsync(
        Guid despesaId, FinancasDaCasa financas, CancellationToken cancellationToken) =>
        Responder(await financas.MarcarComoAPagarAsync(despesaId, cancellationToken));

    private static async Task<Ok<IReadOnlyList<RendaDto>>> ListarRendasAsync(FinancasDaCasa financas, CancellationToken cancellationToken) =>
        TypedResults.Ok(await financas.ListarRendasAsync(cancellationToken));

    private static async Task<Results<Created<RendaDto>, UnprocessableEntity<FalhaDeRegraDto>>> LancarRendaAsync(
        DadosDaNovaRenda dados, FinancasDaCasa financas, CancellationToken cancellationToken)
    {
        var resultado = await financas.LancarRendaAsync(dados, cancellationToken);
        return resultado.Falhou
            ? RespostasDaApi.FalhaDeRegra(resultado)
            : TypedResults.Created("/api/financas/rendas", resultado.Valor);
    }

    private static async Task<Ok<IReadOnlyList<DividaDto>>> ListarDividasAsync(FinancasDaCasa financas, CancellationToken cancellationToken) =>
        TypedResults.Ok(await financas.ListarDividasAsync(cancellationToken));

    private static async Task<Results<Created<DividaDto>, UnprocessableEntity<FalhaDeRegraDto>>> LancarDividaAsync(
        DadosDaNovaDivida dados, FinancasDaCasa financas, CancellationToken cancellationToken)
    {
        var resultado = await financas.LancarDividaAsync(dados, cancellationToken);
        return resultado.Falhou
            ? RespostasDaApi.FalhaDeRegra(resultado)
            : TypedResults.Created("/api/financas/dividas", resultado.Valor);
    }

    private static async Task<Ok<ContribuicaoDoMesDto>> ContribuicaoAsync(
        int? ano, int? mes, FinancasDaCasa financas, CancellationToken cancellationToken)
    {
        var (anoDoFiltro, mesDoFiltro) = MesPedido(ano, mes);
        return TypedResults.Ok(await financas.ContribuicaoDoMesAsync(anoDoFiltro, mesDoFiltro, cancellationToken));
    }

    /// <summary>O mês pedido; sem ano e mês, o mês de hoje no horário de Brasília.</summary>
    internal static (int Ano, int Mes) MesPedido(int? ano, int? mes)
    {
        var hoje = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, HorarioDeBrasilia);
        var anoDoFiltro = ano ?? hoje.Year;
        var mesDoFiltro = mes ?? hoje.Month;
        if (mesDoFiltro is < 1 or > 12 || anoDoFiltro is < 2000 or > 2100)
        {
            throw new DadosInvalidosException("Escolha um mês de 1 a 12 e um ano válido.");
        }

        return (anoDoFiltro, mesDoFiltro);
    }

    private static Results<Ok<DespesaDto>, NotFound, UnprocessableEntity<FalhaDeRegraDto>> Responder(ResultadoDeDominio<DespesaDto>? resultado) =>
        resultado switch
        {
            null => TypedResults.NotFound(),
            { Falhou: true } => RespostasDaApi.FalhaDeRegra(resultado),
            _ => TypedResults.Ok(resultado.Valor),
        };
}
