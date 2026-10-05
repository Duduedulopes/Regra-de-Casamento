using RegraDeCasamento.Application.Acesso;
using RegraDeCasamento.Application.Comum;
using RegraDeCasamento.Domain.Comum;
using RegraDeCasamento.Domain.Familias;

namespace RegraDeCasamento.Application.Familias;

/// <summary>
/// Um adulto da família aprova ou recusa um pedido de entrada (R34). Na aprovação, a conta de quem pediu
/// fica ligada ao novo adulto, tudo gravado de uma vez.
/// </summary>
public sealed class ResponderPedidoDeEntrada(
    IUsuarioAtual usuario,
    IContasDeAcesso contas,
    IRepositorioDeFamilias familias,
    IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task<ResultadoDeDominio> AprovarAsync(Guid pedidoId, CancellationToken cancellationToken = default)
    {
        var (familia, adultoId) = await FamiliaDeQuemRespondeAsync(cancellationToken);

        var pedido = familia.PedidosDeEntrada.FirstOrDefault(p => p.Id == pedidoId);
        if (pedido is { Situacao: SituacaoDoPedido.Pendente }
            && await contas.ObterMembroIdAsync(pedido.ContaDoSolicitanteId, cancellationToken) is not null)
        {
            // Quem pediu entrou em outra família enquanto esperava; uma conta só participa de uma (R1).
            return ResultadoDeDominio.Falha("R1", "Quem pediu já faz parte de outra família.");
        }

        var aprovacao = familia.AprovarPedidoDeEntrada(pedidoId, adultoId);
        if (aprovacao.Falhou)
        {
            return aprovacao;
        }

        await contas.VincularAoMembroAsync(pedido!.ContaDoSolicitanteId, aprovacao.Valor.Id, cancellationToken);
        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        return ResultadoDeDominio.Ok();
    }

    public async Task<ResultadoDeDominio> RecusarAsync(Guid pedidoId, CancellationToken cancellationToken = default)
    {
        var (familia, adultoId) = await FamiliaDeQuemRespondeAsync(cancellationToken);

        var recusa = familia.RecusarPedidoDeEntrada(pedidoId, adultoId);
        if (recusa.Falhou)
        {
            return recusa;
        }

        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        return ResultadoDeDominio.Ok();
    }

    // Só adultos chegam aqui (policy Adulto no servidor), então a família e o membro existem.
    private async Task<(Familia Familia, Guid AdultoId)> FamiliaDeQuemRespondeAsync(CancellationToken cancellationToken)
    {
        var adultoId = usuario.MembroId ?? throw new InvalidOperationException("Responder pedido exige um membro logado.");
        var familia = await familias.ObterDaFamiliaAtualAsync(cancellationToken)
            ?? throw new InvalidOperationException("Responder pedido exige uma família.");
        return (familia, adultoId);
    }
}
