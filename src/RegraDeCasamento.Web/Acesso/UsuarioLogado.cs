using System.Security.Claims;
using RegraDeCasamento.Application.Comum;
using RegraDeCasamento.Shared.Acesso;

namespace RegraDeCasamento.Web.Acesso;

/// <summary>
/// Lê do cookie de login quem está usando o sistema. A família lida aqui é a que o banco usa para filtrar tudo (R5).
/// </summary>
public sealed class UsuarioLogado(IHttpContextAccessor httpContextAccessor) : IUsuarioAtual, IFamiliaAtual
{
    public Guid? ContaId => Ler(ClaimTypes.NameIdentifier);

    public Guid? MembroId => Ler(ClaimsDeAcesso.MembroId);

    public Guid? FamiliaId => Ler(ClaimsDeAcesso.FamiliaId);

    private Guid? Ler(string tipo) =>
        Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirst(tipo)?.Value, out var valor) ? valor : null;
}
