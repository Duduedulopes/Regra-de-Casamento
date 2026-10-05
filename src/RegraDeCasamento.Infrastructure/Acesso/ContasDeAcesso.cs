using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RegraDeCasamento.Application.Acesso;
using RegraDeCasamento.Domain.Comum;
using RegraDeCasamento.Infrastructure.Persistencia;

namespace RegraDeCasamento.Infrastructure.Acesso;

public sealed class ContasDeAcesso(RegraDeCasamentoDbContext db, UserManager<Conta> userManager) : IContasDeAcesso
{
    public Task<Guid?> ObterMembroIdAsync(Guid contaId, CancellationToken cancellationToken = default) =>
        db.Users
            .Where(conta => conta.Id == contaId)
            .Select(conta => conta.MembroId)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task VincularAoMembroAsync(Guid contaId, Guid membroId, CancellationToken cancellationToken = default)
    {
        var conta = await db.Users.SingleAsync(conta => conta.Id == contaId, cancellationToken);
        conta.MembroId = membroId;

        // O Identity usa este carimbo para perceber que a conta mudou.
        conta.ConcurrencyStamp = Guid.NewGuid().ToString();
    }

    public async Task CriarContaDeCriancaAsync(string usuario, string pin, Guid membroId, CancellationToken cancellationToken = default)
    {
        var conta = new Conta { UserName = usuario, MembroId = membroId, TipoDeLogin = TipoDeLogin.ApelidoEPin };

        // O UserManager grava na hora, junto com o que estiver pendente no mesmo DbContext (o membro novo),
        // então criança e conta entram no banco juntas. Se o PIN não servir, nada é gravado.
        var resultado = await userManager.CreateAsync(conta, pin);
        if (!resultado.Succeeded)
        {
            throw new DadosInvalidosException(resultado.Errors.Select(erro => erro.Description).ToList());
        }
    }
}
