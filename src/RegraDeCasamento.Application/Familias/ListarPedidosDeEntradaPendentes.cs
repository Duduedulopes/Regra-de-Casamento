using RegraDeCasamento.Domain.Familias;
using RegraDeCasamento.Shared.Familias;

namespace RegraDeCasamento.Application.Familias;

/// <summary>Os pedidos de entrada que esperam a resposta de um adulto da família (R34), do mais antigo ao mais novo.</summary>
public sealed class ListarPedidosDeEntradaPendentes(IRepositorioDeFamilias familias)
{
    public async Task<IReadOnlyList<PedidoDeEntradaDto>> ExecutarAsync(CancellationToken cancellationToken = default)
    {
        var familia = await familias.ObterDaFamiliaAtualAsync(cancellationToken);
        if (familia is null)
        {
            return [];
        }

        return familia.PedidosDeEntrada
            .Where(pedido => pedido.Situacao == SituacaoDoPedido.Pendente)
            .OrderBy(pedido => pedido.CriadoEmUtc)
            .Select(pedido => new PedidoDeEntradaDto(pedido.Id, pedido.NomeDoSolicitante, pedido.CriadoEmUtc))
            .ToList();
    }
}
