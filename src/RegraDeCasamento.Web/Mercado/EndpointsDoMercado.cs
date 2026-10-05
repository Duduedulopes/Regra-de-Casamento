using Microsoft.AspNetCore.Http.HttpResults;
using RegraDeCasamento.Application.Mercado;
using RegraDeCasamento.Application.Relatorios;
using RegraDeCasamento.Shared.Acesso;
using RegraDeCasamento.Shared.Comum;
using RegraDeCasamento.Shared.Mercado;
using RegraDeCasamento.Web.Financas;

namespace RegraDeCasamento.Web.Mercado;

/// <summary>Compras, resumo e planilha de controle (R12, R13). Só para adultos (R4); a policy é conferida no servidor.</summary>
internal static class EndpointsDoMercado
{
    public static RouteGroupBuilder MapEndpointsDoMercado(this RouteGroupBuilder api)
    {
        var mercado = api.MapGroup("/mercado").RequireAuthorization(Politicas.Adulto);
        mercado.MapGet("/compras", ComprasAsync);
        mercado.MapPost("/compras", LancarAsync);
        mercado.MapGet("/resumo", ResumoAsync);
        mercado.MapGet("/planilha", PlanilhaAsync);
        return api;
    }

    private static async Task<Ok<IReadOnlyList<CompraDto>>> ComprasAsync(
        int? ano, int? mes, MercadoDaFamilia mercado, CancellationToken cancellationToken)
    {
        var (anoDoFiltro, mesDoFiltro) = EndpointsDeFinancas.MesPedido(ano, mes);
        return TypedResults.Ok(await mercado.ComprasDoMesAsync(anoDoFiltro, mesDoFiltro, cancellationToken));
    }

    private static async Task<Results<Created<CompraDto>, UnprocessableEntity<FalhaDeRegraDto>>> LancarAsync(
        DadosDaNovaCompra dados, MercadoDaFamilia mercado, CancellationToken cancellationToken)
    {
        var resultado = await mercado.LancarAsync(dados, cancellationToken);
        return resultado.Falhou
            ? RespostasDaApi.FalhaDeRegra(resultado)
            : TypedResults.Created($"/api/mercado/compras/{resultado.Valor.Id}", resultado.Valor);
    }

    private static async Task<Ok<ResumoDoMercadoDto>> ResumoAsync(
        int? ano, int? mes, MercadoDaFamilia mercado, CancellationToken cancellationToken)
    {
        var (anoDoFiltro, mesDoFiltro) = EndpointsDeFinancas.MesPedido(ano, mes);
        return TypedResults.Ok(await mercado.ResumoAsync(anoDoFiltro, mesDoFiltro, cancellationToken));
    }

    private static async Task<FileContentHttpResult> PlanilhaAsync(
        int? ano, int? mes, MercadoDaFamilia mercado, IGeradorDePlanilhas planilhas, CancellationToken cancellationToken)
    {
        var (anoDoFiltro, mesDoFiltro) = EndpointsDeFinancas.MesPedido(ano, mes);
        var arquivo = await mercado.PlanilhaAsync(anoDoFiltro, mesDoFiltro, planilhas, cancellationToken);
        return TypedResults.File(
            arquivo,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"mercado-{anoDoFiltro}-{mesDoFiltro:00}.xlsx");
    }
}
