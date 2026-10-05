using RegraDeCasamento.Domain.Financas;

namespace RegraDeCasamento.Application.Financas;

/// <summary>Despesas e rendas da família de quem está logado. O filtro por família (R5) já vale em todas as buscas.</summary>
public interface IRepositorioDeFinancas
{
    /// <summary>As despesas que vencem no mês, mais as pagas no mês (mesmo que tenham vencido antes).</summary>
    Task<List<Despesa>> DespesasDoMesAsync(int ano, int mes, CancellationToken cancellationToken = default);

    Task<Despesa?> ObterDespesaAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<Renda>> RendasAsync(CancellationToken cancellationToken = default);

    Task<List<Divida>> DividasAsync(CancellationToken cancellationToken = default);

    /// <summary>Todas as parcelas de dívidas (despesas ligadas a uma dívida), para mostrar o andamento (R38).</summary>
    Task<List<Despesa>> ParcelasDasDividasAsync(CancellationToken cancellationToken = default);

    void Adicionar(Despesa despesa);

    void Adicionar(Renda renda);

    void Adicionar(Divida divida);
}
