using RegraDeCasamento.Domain.Familias;

namespace RegraDeCasamento.Application.Familias;

public interface IRepositorioDeFamilias
{
    /// <summary>A família de quem está logado, com os membros e os pedidos de entrada. Null se não houver.</summary>
    Task<Familia?> ObterDaFamiliaAtualAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// A família dona do código, para receber um pedido de entrada (R34). Quem pede ainda não é da família,
    /// então esta busca passa por cima do filtro por família. O resultado nunca deve ser devolvido a quem pediu.
    /// </summary>
    Task<Familia?> ObterPeloCodigoAsync(string codigo, CancellationToken cancellationToken = default);

    void Adicionar(Familia familia);
}
