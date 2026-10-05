using Microsoft.AspNetCore.Http.HttpResults;
using RegraDeCasamento.Domain.Comum;
using RegraDeCasamento.Shared.Comum;

namespace RegraDeCasamento.Web;

internal static class RespostasDaApi
{
    /// <summary>Regra que não deixou vira 422, com o código da regra e a mensagem.</summary>
    public static UnprocessableEntity<FalhaDeRegraDto> FalhaDeRegra(ResultadoDeDominio falha) =>
        TypedResults.UnprocessableEntity(new FalhaDeRegraDto(falha.CodigoDaRegra!, falha.Mensagem!));
}

/// <summary>Dado que não serve (<see cref="DadosInvalidosException"/>) vira 400 com as mensagens, em qualquer endpoint da API.</summary>
internal sealed class FiltroDeDadosInvalidos : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (DadosInvalidosException erro)
        {
            return TypedResults.BadRequest(new ErrosDeDadosDto(erro.Erros));
        }
    }
}
