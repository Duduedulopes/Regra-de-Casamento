using Microsoft.EntityFrameworkCore;
using RegraDeCasamento.Application.Financas;
using RegraDeCasamento.Domain.Financas;
using RegraDeCasamento.Infrastructure.Persistencia;

namespace RegraDeCasamento.Infrastructure.Financas;

// Sem Where por família: o filtro global já deixa só a família de quem está logado (R5).
public sealed class RepositorioDeFinancas(RegraDeCasamentoDbContext db) : IRepositorioDeFinancas
{
    public Task<List<Despesa>> DespesasDoMesAsync(int ano, int mes, CancellationToken cancellationToken = default)
    {
        var primeiroDia = new DateOnly(ano, mes, 1);
        var proximoMes = primeiroDia.AddMonths(1);
        return db.Despesas
            .Where(d => (d.Vencimento >= primeiroDia && d.Vencimento < proximoMes)
                || (d.PagaEm >= primeiroDia && d.PagaEm < proximoMes))
            .ToListAsync(cancellationToken);
    }

    public Task<Despesa?> ObterDespesaAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Despesas.SingleOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<List<Renda>> RendasAsync(CancellationToken cancellationToken = default) =>
        db.Rendas.ToListAsync(cancellationToken);

    public Task<List<Divida>> DividasAsync(CancellationToken cancellationToken = default) =>
        db.Dividas.ToListAsync(cancellationToken);

    public Task<List<Despesa>> ParcelasDasDividasAsync(CancellationToken cancellationToken = default) =>
        db.Despesas.Where(d => d.DividaId != null).ToListAsync(cancellationToken);

    public void Adicionar(Despesa despesa) => db.Despesas.Add(despesa);

    public void Adicionar(Renda renda) => db.Rendas.Add(renda);

    public void Adicionar(Divida divida) => db.Dividas.Add(divida);
}
