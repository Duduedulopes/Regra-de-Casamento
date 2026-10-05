using Microsoft.EntityFrameworkCore;
using RegraDeCasamento.Application.Mercado;
using RegraDeCasamento.Domain.Mercado;
using RegraDeCasamento.Infrastructure.Persistencia;

namespace RegraDeCasamento.Infrastructure.Mercado;

// Sem Where por família: o filtro global já deixa só a família de quem está logado (R5).
// Os itens são parte da compra (owned), então vêm junto e seguem o mesmo filtro.
public sealed class RepositorioDeCompras(RegraDeCasamentoDbContext db) : IRepositorioDeCompras
{
    public Task<List<Compra>> ComprasDoMesAsync(int ano, int mes, CancellationToken cancellationToken = default)
    {
        var primeiroDia = new DateOnly(ano, mes, 1);
        var proximoMes = primeiroDia.AddMonths(1);
        return db.Compras
            .Where(compra => compra.Dia >= primeiroDia && compra.Dia < proximoMes)
            .ToListAsync(cancellationToken);
    }

    public void Adicionar(Compra compra) => db.Compras.Add(compra);
}
