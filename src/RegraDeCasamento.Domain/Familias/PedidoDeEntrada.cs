using RegraDeCasamento.Domain.Comum;

namespace RegraDeCasamento.Domain.Familias;

/// <summary>
/// O pedido de um adulto para entrar numa família que já existe. Sem a aprovação de um adulto da família,
/// ninguém entra (R34).
/// </summary>
public sealed class PedidoDeEntrada : Entidade, IPertenceAFamilia
{
    private PedidoDeEntrada()
    {
    }

    public Guid FamiliaId { get; private set; }

    /// <summary>A conta de login de quem pediu. Se o pedido for aprovado, essa conta passa a ser do novo membro.</summary>
    public Guid ContaDoSolicitanteId { get; private set; }

    public string NomeDoSolicitante { get; private set; } = "";

    public SituacaoDoPedido Situacao { get; private set; }

    /// <summary>O adulto da família que aprovou ou recusou. Vazio enquanto o pedido espera resposta.</summary>
    public Guid? RespondidoPorId { get; private set; }

    internal static PedidoDeEntrada Novo(Guid familiaId, Guid contaDoSolicitanteId, string nomeDoSolicitante) => new()
    {
        FamiliaId = familiaId,
        ContaDoSolicitanteId = contaDoSolicitanteId,
        NomeDoSolicitante = nomeDoSolicitante,
        Situacao = SituacaoDoPedido.Pendente,
    };

    internal void Aprovar(Guid adultoId) => Responder(SituacaoDoPedido.Aprovado, adultoId);

    internal void Recusar(Guid adultoId) => Responder(SituacaoDoPedido.Recusado, adultoId);

    private void Responder(SituacaoDoPedido situacao, Guid adultoId)
    {
        Situacao = situacao;
        RespondidoPorId = adultoId;
        RegistrarAtualizacao();
    }
}
