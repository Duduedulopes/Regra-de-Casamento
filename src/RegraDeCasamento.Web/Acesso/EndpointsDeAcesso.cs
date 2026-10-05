using System.Net.Mail;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using RegraDeCasamento.Infrastructure.Acesso;
using RegraDeCasamento.Shared.Acesso;
using RegraDeCasamento.Shared.Comum;

namespace RegraDeCasamento.Web.Acesso;

internal static class EndpointsDeAcesso
{
    public static RouteGroupBuilder MapEndpointsDeAcesso(this RouteGroupBuilder api)
    {
        var acesso = api.MapGroup("/acesso");
        acesso.MapPost("/cadastrar", CadastrarAsync).AllowAnonymous();
        acesso.MapPost("/entrar", EntrarAsync).AllowAnonymous();
        acesso.MapPost("/entrar-crianca", EntrarCriancaAsync).AllowAnonymous();
        acesso.MapPost("/sair", SairAsync).RequireAuthorization();
        acesso.MapPost("/atualizar", AtualizarAsync).RequireAuthorization();
        acesso.MapGet("/eu", QuemSouEu).RequireAuthorization();
        return api;
    }

    /// <summary>Um adulto cria a conta com e-mail e senha e já fica logado. A família vem depois.</summary>
    private static async Task<Results<NoContent, BadRequest<ErrosDeDadosDto>>> CadastrarAsync(
        DadosDeCadastro dados,
        UserManager<Conta> userManager,
        SignInManager<Conta> signInManager)
    {
        var email = dados.Email?.Trim() ?? "";
        if (!MailAddress.TryCreate(email, out var endereco) || endereco.Address != email)
        {
            return TypedResults.BadRequest(new ErrosDeDadosDto(["Informe um e-mail válido."]));
        }

        var conta = new Conta { UserName = email, Email = email };
        var resultado = await userManager.CreateAsync(conta, dados.Senha ?? "");
        if (!resultado.Succeeded)
        {
            return TypedResults.BadRequest(new ErrosDeDadosDto(resultado.Errors.Select(erro => erro.Description).ToList()));
        }

        await signInManager.SignInAsync(conta, isPersistent: true);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, UnauthorizedHttpResult>> EntrarAsync(
        DadosDeLogin dados,
        SignInManager<Conta> signInManager)
    {
        if (string.IsNullOrWhiteSpace(dados.Usuario) || string.IsNullOrEmpty(dados.Senha))
        {
            return TypedResults.Unauthorized();
        }

        // Cookie persistente: o app fica logado nos aparelhos da família. Muitas senhas erradas bloqueiam a conta por um tempo.
        var resultado = await signInManager.PasswordSignInAsync(dados.Usuario.Trim(), dados.Senha, isPersistent: true, lockoutOnFailure: true);
        return resultado.Succeeded ? TypedResults.NoContent() : TypedResults.Unauthorized();
    }

    /// <summary>A criança entra com o código da família, o apelido e o PIN (R35). Errar o PIN muitas vezes bloqueia a conta por um tempo.</summary>
    private static Task<Results<NoContent, UnauthorizedHttpResult>> EntrarCriancaAsync(
        DadosDeLoginDaCrianca dados,
        SignInManager<Conta> signInManager) =>
        EntrarAsync(new DadosDeLogin(DadosDeLoginDaCrianca.UsuarioDaCrianca(dados.Apelido, dados.CodigoDaFamilia), dados.Pin), signInManager);

    private static async Task<NoContent> SairAsync(SignInManager<Conta> signInManager)
    {
        await signInManager.SignOutAsync();
        return TypedResults.NoContent();
    }

    /// <summary>
    /// Refaz o cookie com o que está no banco agora. Serve para quem acabou de ter o pedido de entrada aprovado
    /// ver a família sem precisar sair e entrar de novo.
    /// </summary>
    internal static async Task<Results<NoContent, UnauthorizedHttpResult>> AtualizarAsync(
        ClaimsPrincipal usuario,
        UserManager<Conta> userManager,
        SignInManager<Conta> signInManager)
    {
        var conta = await userManager.GetUserAsync(usuario);
        if (conta is null)
        {
            return TypedResults.Unauthorized();
        }

        await signInManager.RefreshSignInAsync(conta);
        return TypedResults.NoContent();
    }

    private static Ok<MeuAcessoDto> QuemSouEu(ClaimsPrincipal usuario) =>
        TypedResults.Ok(new MeuAcessoDto(
            usuario.Identity?.Name ?? "",
            LerGuid(usuario, ClaimsDeAcesso.MembroId),
            LerGuid(usuario, ClaimsDeAcesso.FamiliaId),
            Enum.TryParse<PerfilDeAcesso>(usuario.FindFirst(ClaimsDeAcesso.Perfil)?.Value, out var perfil) ? perfil : null));

    private static Guid? LerGuid(ClaimsPrincipal usuario, string tipo) =>
        Guid.TryParse(usuario.FindFirst(tipo)?.Value, out var valor) ? valor : null;
}
