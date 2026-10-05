using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace RegraDeCasamento.Web.Acesso;

internal static class CookieDeLogin
{
    /// <summary>
    /// Na API, quem não está logado recebe 401 e quem não tem permissão recebe 403, em vez de ser
    /// mandado para uma página de login. As páginas continuam com o redirecionamento normal.
    /// </summary>
    public static void Configurar(CookieAuthenticationOptions opcoes)
    {
        opcoes.Events.OnRedirectToLogin = contexto => Responder(contexto, StatusCodes.Status401Unauthorized);
        opcoes.Events.OnRedirectToAccessDenied = contexto => Responder(contexto, StatusCodes.Status403Forbidden);
    }

    private static Task Responder(RedirectContext<CookieAuthenticationOptions> contexto, int statusDaApi)
    {
        if (contexto.Request.Path.StartsWithSegments(EndpointsDaApi.Prefixo))
        {
            contexto.Response.StatusCode = statusDaApi;
        }
        else
        {
            contexto.Response.Redirect(contexto.RedirectUri);
        }

        return Task.CompletedTask;
    }
}
