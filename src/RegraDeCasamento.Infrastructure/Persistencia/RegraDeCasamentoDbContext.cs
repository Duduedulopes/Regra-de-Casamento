using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RegraDeCasamento.Application.Comum;
using RegraDeCasamento.Domain.Comum;
using RegraDeCasamento.Domain.Familias;
using RegraDeCasamento.Domain.Financas;
using RegraDeCasamento.Domain.Mercado;
using RegraDeCasamento.Infrastructure.Acesso;

namespace RegraDeCasamento.Infrastructure.Persistencia;

/// <summary>
/// O banco inteiro num contexto só: as tabelas do Domain e as contas de login (Identity, sem tabelas de papéis,
/// porque os perfis Adulto e Crianca vêm do membro).
/// </summary>
public sealed class RegraDeCasamentoDbContext(DbContextOptions<RegraDeCasamentoDbContext> options, IFamiliaAtual familiaAtual)
    : IdentityUserContext<Conta, Guid>(options), IUnidadeDeTrabalho
{
    public DbSet<Familia> Familias => Set<Familia>();

    public DbSet<Membro> Membros => Set<Membro>();

    public DbSet<PedidoDeEntrada> PedidosDeEntrada => Set<PedidoDeEntrada>();

    public DbSet<Despesa> Despesas => Set<Despesa>();

    public DbSet<Renda> Rendas => Set<Renda>();

    public DbSet<Compra> Compras => Set<Compra>();

    public DbSet<Divida> Dividas => Set<Divida>();

    /// <summary>
    /// Lido pelo filtro global a cada consulta (R5). Sem família logada vale Guid.Empty, e nada aparece.
    /// </summary>
    public Guid FamiliaIdAtual => familiaAtual.FamiliaId ?? Guid.Empty;

    public Task SalvarAsync(CancellationToken cancellationToken = default) => SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RegraDeCasamentoDbContext).Assembly);

        foreach (var tipo in modelBuilder.Model.GetEntityTypes().Select(entidade => entidade.ClrType).ToList())
        {
            if (typeof(Entidade).IsAssignableFrom(tipo))
            {
                // O Id nasce no C# (STACK.md, regra 2).
                modelBuilder.Entity(tipo).Property(nameof(Entidade.Id)).ValueGeneratedNever();
            }

            if (typeof(IPertenceAFamilia).IsAssignableFrom(tipo))
            {
                modelBuilder.Entity(tipo).HasQueryFilter(FiltroDaFamiliaAtual(tipo));
            }
        }

        // A própria família não tem FamiliaId: cada uma só enxerga a si mesma.
        modelBuilder.Entity<Familia>().HasQueryFilter(familia => familia.Id == FamiliaIdAtual);
    }

    /// <summary>Monta "e => e.FamiliaId == FamiliaIdAtual" para o tipo, como se fosse escrito à mão.</summary>
    private LambdaExpression FiltroDaFamiliaAtual(Type tipo)
    {
        var entidade = Expression.Parameter(tipo, "entidade");
        var familiaDaEntidade = Expression.Property(entidade, nameof(IPertenceAFamilia.FamiliaId));

        // Referenciar o próprio DbContext faz o EF ler o valor do contexto que está consultando, não deste aqui.
        var familiaLogada = Expression.Property(Expression.Constant(this), nameof(FamiliaIdAtual));

        return Expression.Lambda(Expression.Equal(familiaDaEntidade, familiaLogada), entidade);
    }
}
