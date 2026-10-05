using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RegraDeCasamento.Infrastructure.Acesso;

namespace RegraDeCasamento.Web.Tests.Acesso;

public sealed class ContasTests(AplicacaoDeTeste aplicacao) : IClassFixture<AplicacaoDeTeste>
{
    [Fact]
    public async Task R1_UmMembroNaoTemDuasContas()
    {
        using var escopo = aplicacao.Services.CreateScope();
        var contas = escopo.ServiceProvider.GetRequiredService<UserManager<Conta>>();
        var segundaConta = new Conta { UserName = "outra.conta@teste.com", MembroId = aplicacao.AdultaId };

        await Assert.ThrowsAsync<DbUpdateException>(() => contas.CreateAsync(segundaConta, AplicacaoDeTeste.Senha));
    }
}
