using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RegraDeCasamento.Domain.Familias;
using RegraDeCasamento.Infrastructure.Acesso;
using RegraDeCasamento.Infrastructure.Persistencia;

namespace RegraDeCasamento.Web.Tests;

/// <summary>
/// O app de verdade (mesmo Program.cs), só que com um banco SQLite em memória no lugar do PostgreSQL.
/// Já nasce com uma família de um adulto e uma criança, cada um com sua conta.
/// </summary>
public sealed class AplicacaoDeTeste : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Senha = "Senha!123";
    public const string UsuarioAdulto = "adulta@teste.com";
    public const string UsuarioCrianca = "lia.teste";

    private readonly SqliteConnection _conexao = new("DataSource=:memory:");

    public Guid FamiliaId { get; private set; }

    public Guid AdultaId { get; private set; }

    public async Task InitializeAsync()
    {
        using var escopo = Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<RegraDeCasamentoDbContext>();
        await db.Database.EnsureCreatedAsync();

        var familia = Familia.Criar("Família de Teste", "Ana");
        var adulta = familia.Membros.Single();
        var crianca = familia.AdicionarCrianca(adulta.Id, "Lia", "lia").Valor;
        familia.ReceberPedidoDeEntrada(Guid.CreateVersion7(), "Bruno");
        db.Familias.Add(familia);
        await db.SaveChangesAsync();
        FamiliaId = familia.Id;
        AdultaId = adulta.Id;

        var contas = escopo.ServiceProvider.GetRequiredService<UserManager<Conta>>();
        await CriarContaAsync(contas, UsuarioAdulto, adulta.Id);
        await CriarContaAsync(contas, UsuarioCrianca, crianca.Id);
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    public HttpClient CriarCliente() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true,
    });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _conexao.Open();

        builder.ConfigureServices(servicos =>
        {
            servicos.RemoveAll<DbContextOptions<RegraDeCasamentoDbContext>>();
            servicos.RemoveAll<IDbContextOptionsConfiguration<RegraDeCasamentoDbContext>>();
            servicos.AddDbContext<RegraDeCasamentoDbContext>(opcoes => opcoes.UseSqlite(_conexao));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _conexao.Dispose();
        }
    }

    private static async Task CriarContaAsync(UserManager<Conta> contas, string usuario, Guid membroId)
    {
        var resultado = await contas.CreateAsync(new Conta { UserName = usuario, MembroId = membroId }, Senha);
        if (!resultado.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", resultado.Errors.Select(erro => erro.Description)));
        }
    }
}
