using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using RegraDeCasamento.Application.Familias;
using RegraDeCasamento.Infrastructure.Acesso;
using RegraDeCasamento.Shared.Comum;
using RegraDeCasamento.Shared.Familias;

namespace RegraDeCasamento.Web.Familias;

/// <summary>O que uma conta logada faz antes de ter família: criar uma ou pedir para entrar numa.</summary>
internal static class EndpointsDasFamilias
{
    public static RouteGroupBuilder MapEndpointsDasFamilias(this RouteGroupBuilder api)
    {
        var familias = api.MapGroup("/familias").RequireAuthorization();
        familias.MapPost("/", CriarAsync);
        familias.MapPost("/entrar", PedirEntradaAsync);
        return api;
    }

    /// <summary>Depois da aprovação, quem pediu chama <c>/api/acesso/atualizar</c> para o login já mostrar a família.</summary>
    private static async Task<Results<Ok<PedidoEnviadoDto>, UnprocessableEntity<FalhaDeRegraDto>>> PedirEntradaAsync(
        DadosDoPedidoDeEntrada dados,
        PedirEntradaNaFamilia casoDeUso,
        CancellationToken cancellationToken)
    {
        var resultado = await casoDeUso.ExecutarAsync(dados, cancellationToken);
        return resultado.Falhou ? RespostasDaApi.FalhaDeRegra(resultado) : TypedResults.Ok(resultado.Valor);
    }

    private static async Task<Results<Created<FamiliaDto>, UnprocessableEntity<FalhaDeRegraDto>>> CriarAsync(
        DadosDaNovaFamilia dados,
        CriarFamilia casoDeUso,
        ClaimsPrincipal usuario,
        UserManager<Conta> userManager,
        SignInManager<Conta> signInManager,
        CancellationToken cancellationToken)
    {
        var resultado = await casoDeUso.ExecutarAsync(dados, cancellationToken);
        if (resultado.Falhou)
        {
            return RespostasDaApi.FalhaDeRegra(resultado);
        }

        // Quem criou já sai daqui como Adulto da família nova, sem precisar entrar de novo.
        var conta = await userManager.GetUserAsync(usuario);
        await signInManager.RefreshSignInAsync(conta!);

        return TypedResults.Created("/api/familia", resultado.Valor);
    }
}
