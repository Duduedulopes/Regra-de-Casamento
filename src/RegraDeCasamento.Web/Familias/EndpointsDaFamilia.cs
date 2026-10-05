using Microsoft.AspNetCore.Http.HttpResults;
using RegraDeCasamento.Application.Familias;
using RegraDeCasamento.Domain.Comum;
using RegraDeCasamento.Shared.Acesso;
using RegraDeCasamento.Shared.Comum;
using RegraDeCasamento.Shared.Familias;

namespace RegraDeCasamento.Web.Familias;

/// <summary>A família de quem está logado. Tudo aqui é só para adultos (R4); a policy é conferida no servidor.</summary>
internal static class EndpointsDaFamilia
{
    public static RouteGroupBuilder MapEndpointsDaFamilia(this RouteGroupBuilder api)
    {
        var familia = api.MapGroup("/familia").RequireAuthorization(Politicas.Adulto);
        familia.MapGet("/", VerAsync);
        familia.MapGet("/pedidos-de-entrada", ListarPedidosDeEntradaAsync);
        familia.MapPost("/pedidos-de-entrada/{pedidoId:guid}/aprovar", AprovarPedidoAsync);
        familia.MapPost("/pedidos-de-entrada/{pedidoId:guid}/recusar", RecusarPedidoAsync);
        familia.MapPost("/criancas", AdicionarCriancaAsync);
        return api;
    }

    private static async Task<Results<Created<CriancaCriadaDto>, UnprocessableEntity<FalhaDeRegraDto>>> AdicionarCriancaAsync(
        DadosDaNovaCrianca dados,
        AdicionarCrianca casoDeUso,
        CancellationToken cancellationToken)
    {
        var resultado = await casoDeUso.ExecutarAsync(dados, cancellationToken);
        return resultado.Falhou
            ? RespostasDaApi.FalhaDeRegra(resultado)
            : TypedResults.Created("/api/familia", resultado.Valor);
    }

    private static async Task<Results<NoContent, UnprocessableEntity<FalhaDeRegraDto>>> AprovarPedidoAsync(
        Guid pedidoId,
        ResponderPedidoDeEntrada casoDeUso,
        CancellationToken cancellationToken) =>
        Responder(await casoDeUso.AprovarAsync(pedidoId, cancellationToken));

    private static async Task<Results<NoContent, UnprocessableEntity<FalhaDeRegraDto>>> RecusarPedidoAsync(
        Guid pedidoId,
        ResponderPedidoDeEntrada casoDeUso,
        CancellationToken cancellationToken) =>
        Responder(await casoDeUso.RecusarAsync(pedidoId, cancellationToken));

    private static Results<NoContent, UnprocessableEntity<FalhaDeRegraDto>> Responder(ResultadoDeDominio resultado) =>
        resultado.Falhou ? RespostasDaApi.FalhaDeRegra(resultado) : TypedResults.NoContent();

    private static async Task<Results<Ok<FamiliaDto>, NotFound>> VerAsync(
        VerMinhaFamilia casoDeUso,
        CancellationToken cancellationToken) =>
        await casoDeUso.ExecutarAsync(cancellationToken) is { } familia
            ? TypedResults.Ok(familia)
            : TypedResults.NotFound();

    private static async Task<Ok<IReadOnlyList<PedidoDeEntradaDto>>> ListarPedidosDeEntradaAsync(
        ListarPedidosDeEntradaPendentes casoDeUso,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await casoDeUso.ExecutarAsync(cancellationToken));
}
