namespace RegraDeCasamento.Domain.Tarefas;

/// <summary>
/// Os pontos de um membro: 1 por tarefa cumprida (R17), menos o que pagou por trocas aceitas (R18).
/// Ponto nunca vira dinheiro (R22): não existe ligação nenhuma com as finanças.
/// </summary>
public static class Pontos
{
    public static int De(Guid membroId, IEnumerable<Tarefa> tarefas, IEnumerable<PedidoDeTroca> trocas)
    {
        var ganhos = tarefas.Count(t => t.ResponsavelId == membroId && t.Situacao == SituacaoDaTarefa.Feita);
        var gastos = trocas.Where(t => t.PedidoPorId == membroId && t.Situacao == SituacaoDaTroca.Aceita).Sum(t => t.Custo);
        return ganhos - gastos;
    }
}
