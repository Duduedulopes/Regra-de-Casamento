using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RegraDeCasamento.Application.Comum;
using RegraDeCasamento.Domain.Familias;
using RegraDeCasamento.Infrastructure.Persistencia;

namespace RegraDeCasamento.Infrastructure.Tests.Persistencia;

/// <summary>
/// Um banco SQLite em memória com o mesmo modelo do PostgreSQL. Cada teste abre contextos "logados"
/// em famílias diferentes sobre o mesmo banco.
/// </summary>
public sealed class BancoEmMemoria : IDisposable
{
    private readonly SqliteConnection _conexao = new("DataSource=:memory:");
    private readonly DbContextOptions<RegraDeCasamentoDbContext> _opcoes;

    public BancoEmMemoria()
    {
        _conexao.Open();
        _opcoes = new DbContextOptionsBuilder<RegraDeCasamentoDbContext>().UseSqlite(_conexao).Options;

        using var contexto = ContextoDaFamilia(null);
        contexto.Database.EnsureCreated();
    }

    public RegraDeCasamentoDbContext ContextoDaFamilia(Guid? familiaId) => new(_opcoes, new FamiliaFixa(familiaId));

    public async Task<Familia> GravarAsync(Familia familia)
    {
        await using var contexto = ContextoDaFamilia(familia.Id);
        contexto.Familias.Add(familia);
        await contexto.SaveChangesAsync();
        return familia;
    }

    /// <summary>Uma família completa: um adulto, uma criança e um pedido de entrada esperando resposta.</summary>
    public static Familia MontarFamilia(string nome, string adulto, string apelidoDaCrianca)
    {
        var familia = Familia.Criar(nome, adulto);
        familia.AdicionarCrianca(familia.Membros.Single().Id, $"Criança {nome}", apelidoDaCrianca);
        familia.ReceberPedidoDeEntrada(Guid.CreateVersion7(), $"Visitante {nome}");
        return familia;
    }

    public void Dispose() => _conexao.Dispose();

    private sealed class FamiliaFixa(Guid? familiaId) : IFamiliaAtual
    {
        public Guid? FamiliaId => familiaId;
    }
}
