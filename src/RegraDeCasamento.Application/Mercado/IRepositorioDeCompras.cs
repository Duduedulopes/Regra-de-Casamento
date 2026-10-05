using RegraDeCasamento.Domain.Mercado;

namespace RegraDeCasamento.Application.Mercado;

/// <summary>As compras da família de quem está logado. O filtro por família (R5) já vale em todas as buscas.</summary>
public interface IRepositorioDeCompras
{
    /// <summary>As compras do mês, com os itens.</summary>
    Task<List<Compra>> ComprasDoMesAsync(int ano, int mes, CancellationToken cancellationToken = default);

    void Adicionar(Compra compra);
}
