using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RegraDeCasamento.Application.Familias;
using RegraDeCasamento.Infrastructure.Acesso;
using RegraDeCasamento.Infrastructure.Persistencia;
using RegraDeCasamento.Shared.Acesso;

namespace RegraDeCasamento.Web.Acesso;

/// <summary>
/// Monta o que o cookie de login guarda: além da conta, o membro, a família e o perfil (Adulto ou Crianca).
/// O servidor confere as permissões com essas informações (ARCHITECTURE.md, 5.2).
/// </summary>
public sealed class FabricaDeClaims(
    UserManager<Conta> userManager,
    IOptions<IdentityOptions> optionsAccessor,
    RegraDeCasamentoDbContext db)
    : UserClaimsPrincipalFactory<Conta>(userManager, optionsAccessor)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(Conta user)
    {
        var identidade = await base.GenerateClaimsAsync(user);
        if (user.MembroId is not { } membroId)
        {
            return identidade;
        }

        // Durante o login ainda não há família no contexto, e o filtro global esconderia o membro.
        // Ignorar o filtro aqui é seguro porque a busca é só pelo membro desta conta.
        var membro = await db.Membros
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(m => m.Id == membroId);

        if (membro is null)
        {
            return identidade;
        }

        identidade.AddClaim(new Claim(ClaimsDeAcesso.MembroId, membro.Id.ToString()));
        identidade.AddClaim(new Claim(ClaimsDeAcesso.FamiliaId, membro.FamiliaId.ToString()));
        identidade.AddClaim(new Claim(ClaimsDeAcesso.Perfil, MapeamentoDeFamilia.PerfilDeAcessoDo(membro).ToString()));
        return identidade;
    }
}
