using RegraDeCasamento.Domain.Comum;
using RegraDeCasamento.Domain.Familias;

namespace RegraDeCasamento.Domain.Tarefas;

public enum SituacaoDaTroca
{
    Pendente,
    Aceita,
    Recusada,
}

/// <summary>
/// Troca de função (R18): o responsável pede para outro membro fazer a tarefa e paga pontos por isso.
/// Sem o aceite do outro, nada muda.
/// </summary>
public sealed class PedidoDeTroca : Entidade, IPertenceAFamilia
{
    /// <summary>Quanto custa pedir uma troca. Decisão em aberto no BUSINESS_RULES.md; padrão provisório: 1 ponto.</summary>
    public const int CustoEmPontos = 1;

    private PedidoDeTroca()
    {
    }

    public Guid FamiliaId { get; private set; }

    public Guid TarefaId { get; private set; }

    /// <summary>Quem pediu (o responsável da tarefa) e paga os pontos se a troca for aceita.</summary>
    public Guid PedidoPorId { get; private set; }

    /// <summary>Quem vai fazer a tarefa, se aceitar.</summary>
    public Guid ParaId { get; private set; }

    public int Custo { get; private set; }

    public SituacaoDaTroca Situacao { get; private set; }

    /// <param name="pontosDeQuemPede">Os pontos que quem pede tem agora (R17, R18).</param>
    public static ResultadoDeDominio<PedidoDeTroca> Pedir(Familia familia, Tarefa tarefa, Guid quemPedeId, Guid paraId, int pontosDeQuemPede)
    {
        if (tarefa.FamiliaId != familia.Id || familia.Membros.All(m => m.Id != quemPedeId))
        {
            return ResultadoDeDominio.Falha<PedidoDeTroca>("R5", "Esta tarefa não é desta família.");
        }

        if (tarefa.ResponsavelId != quemPedeId)
        {
            return ResultadoDeDominio.Falha<PedidoDeTroca>("R18", "Só o responsável pela tarefa pede a troca.");
        }

        if (paraId == quemPedeId || familia.Membros.All(m => m.Id != paraId))
        {
            return ResultadoDeDominio.Falha<PedidoDeTroca>("R18", "Escolha outra pessoa da família para fazer a tarefa.");
        }

        if (tarefa.Situacao == SituacaoDaTarefa.Feita)
        {
            return ResultadoDeDominio.Falha<PedidoDeTroca>("R18", "A tarefa já foi feita; não há o que trocar.");
        }

        if (pontosDeQuemPede < CustoEmPontos)
        {
            return ResultadoDeDominio.Falha<PedidoDeTroca>("R18", $"Pedir uma troca custa {CustoEmPontos} ponto, e você ainda não tem pontos suficientes.");
        }

        return ResultadoDeDominio.Ok(new PedidoDeTroca
        {
            FamiliaId = familia.Id,
            TarefaId = tarefa.Id,
            PedidoPorId = quemPedeId,
            ParaId = paraId,
            Custo = CustoEmPontos,
            Situacao = SituacaoDaTroca.Pendente,
        });
    }

    /// <summary>
    /// Quem recebeu o pedido aceita: a tarefa passa para ele, e quem pediu paga os pontos (R18).
    /// Os pontos são conferidos de novo, porque podem ter sido gastos enquanto o pedido esperava.
    /// </summary>
    public ResultadoDeDominio Aceitar(Guid quemRespondeId, Tarefa tarefa, int pontosDeQuemPediu)
    {
        var falha = ConferirResposta(quemRespondeId, tarefa);
        if (falha is not null)
        {
            return falha;
        }

        if (tarefa.ResponsavelId != PedidoPorId || tarefa.Situacao == SituacaoDaTarefa.Feita)
        {
            return ResultadoDeDominio.Falha("R18", "A tarefa mudou desde o pedido; a troca não vale mais.");
        }

        if (pontosDeQuemPediu < Custo)
        {
            return ResultadoDeDominio.Falha("R18", "Quem pediu não tem mais pontos para pagar a troca.");
        }

        tarefa.PassarPara(ParaId);
        Situacao = SituacaoDaTroca.Aceita;
        RegistrarAtualizacao();
        return ResultadoDeDominio.Ok();
    }

    /// <summary>Quem recebeu o pedido recusa: nada muda e ninguém paga (R18).</summary>
    public ResultadoDeDominio Recusar(Guid quemRespondeId, Tarefa tarefa)
    {
        var falha = ConferirResposta(quemRespondeId, tarefa);
        if (falha is not null)
        {
            return falha;
        }

        Situacao = SituacaoDaTroca.Recusada;
        RegistrarAtualizacao();
        return ResultadoDeDominio.Ok();
    }

    private ResultadoDeDominio? ConferirResposta(Guid quemRespondeId, Tarefa tarefa)
    {
        if (tarefa.Id != TarefaId)
        {
            return ResultadoDeDominio.Falha("R5", "Esta tarefa não é a do pedido.");
        }

        if (quemRespondeId != ParaId)
        {
            return ResultadoDeDominio.Falha("R18", "Só quem recebeu o pedido aceita ou recusa a troca.");
        }

        return Situacao == SituacaoDaTroca.Pendente
            ? null
            : ResultadoDeDominio.Falha("R18", "Este pedido de troca já foi respondido.");
    }
}
