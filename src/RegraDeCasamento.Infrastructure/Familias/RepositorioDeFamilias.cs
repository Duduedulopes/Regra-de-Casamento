using Microsoft.EntityFrameworkCore;
using RegraDeCasamento.Application.Familias;
using RegraDeCasamento.Domain.Familias;
using RegraDeCasamento.Infrastructure.Persistencia;

namespace RegraDeCasamento.Infrastructure.Familias;

public sealed class RepositorioDeFamilias(RegraDeCasamentoDbContext db) : IRepositorioDeFamilias
{
    // Sem Where: o filtro global já deixa só a família de quem está logado (R5).
    public Task<Familia?> ObterDaFamiliaAtualAsync(CancellationToken cancellationToken = default) =>
        ComMembrosEPedidos(db.Familias)
            .SingleOrDefaultAsync(cancellationToken);

    // Exceção ao filtro anotada na ARCHITECTURE.md (5.1): quem pede entrada ainda não é da família.
    public Task<Familia?> ObterPeloCodigoAsync(string codigo, CancellationToken cancellationToken = default)
    {
        var codigoNormalizado = Familia.NormalizarCodigo(codigo);
        return ComMembrosEPedidos(db.Familias.IgnoreQueryFilters())
            .SingleOrDefaultAsync(familia => familia.Codigo == codigoNormalizado, cancellationToken);
    }

    public void Adicionar(Familia familia) => db.Familias.Add(familia);

    private static IQueryable<Familia> ComMembrosEPedidos(IQueryable<Familia> familias) =>
        familias
            .Include(familia => familia.Membros)
            .Include(familia => familia.PedidosDeEntrada)
            .AsSplitQuery();
}
