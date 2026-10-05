using RegraDeCasamento.Domain.Comum;
using RegraDeCasamento.Domain.Familias;

namespace RegraDeCasamento.Domain.Tarefas;

public sealed record PosicaoNoRanking(Guid MembroId, string Nome, int Atribuidas, int Feitas, decimal Porcentagem);

/// <summary>
/// O ranking do mês (R19): vale a <b>porcentagem</b> das tarefas cumpridas, não o total de pontos,
/// para ser justo com quem não mora o tempo todo na casa.
/// </summary>
public sealed record RankingDoMes(int Ano, int Mes, IReadOnlyList<PosicaoNoRanking> Posicoes)
{
    /// <summary>
    /// O TOP 1 do mês. Desempate (decisão em aberto, padrão provisório): mais tarefas feitas; se ainda empatar,
    /// os empatados dividem o TOP 1. Sem nenhuma tarefa feita no mês, não há TOP 1.
    /// </summary>
    public IReadOnlyList<PosicaoNoRanking> Top1
    {
        get
        {
            var primeiro = Posicoes.FirstOrDefault();
            if (primeiro is null || primeiro.Feitas == 0)
            {
                return [];
            }

            return Posicoes.Where(p => p.Porcentagem == primeiro.Porcentagem && p.Feitas == primeiro.Feitas).ToList();
        }
    }

    /// <summary>Entra no ranking quem teve pelo menos uma tarefa no mês (com o responsável de hoje).</summary>
    public static RankingDoMes Calcular(Familia familia, IEnumerable<Tarefa> tarefas, int ano, int mes)
    {
        var doMes = tarefas
            .Where(t => t.FamiliaId == familia.Id && t.Dia.Year == ano && t.Dia.Month == mes)
            .ToList();

        var posicoes = familia.Membros
            .Select(membro =>
            {
                var dele = doMes.Where(t => t.ResponsavelId == membro.Id).ToList();
                var feitas = dele.Count(t => t.Situacao == SituacaoDaTarefa.Feita);
                var porcentagem = dele.Count == 0
                    ? 0
                    : decimal.Round(100m * feitas / dele.Count, 1, MidpointRounding.AwayFromZero);
                return new PosicaoNoRanking(membro.Id, membro.Nome, dele.Count, feitas, porcentagem);
            })
            .Where(p => p.Atribuidas > 0)
            .OrderByDescending(p => p.Porcentagem)
            .ThenByDescending(p => p.Feitas)
            .ThenBy(p => p.Nome, StringComparer.CurrentCulture)
            .ToList();

        return new RankingDoMes(ano, mes, posicoes);
    }
}

/// <summary>
/// Um TOP 1 guardado no mural (R20). O mural guarda todos os meses anteriores; a recompensa é escolher o
/// lanche de sábado à noite, ou outro dia combinado (R21).
/// </summary>
public sealed class TopDoMes : Entidade, IPertenceAFamilia
{
    private TopDoMes()
    {
    }

    public Guid FamiliaId { get; private set; }

    public int Ano { get; private set; }

    public int Mes { get; private set; }

    public Guid MembroId { get; private set; }

    /// <summary>O nome no dia em que entrou no mural, para o mural não mudar se o nome mudar depois.</summary>
    public string Nome { get; private set; } = "";

    public decimal Porcentagem { get; private set; }

    public int Feitas { get; private set; }

    /// <summary>Guarda no mural os TOP 1 de um mês que já acabou (R20). Mês em andamento ainda não entra.</summary>
    public static IReadOnlyList<TopDoMes> ParaOMural(RankingDoMes ranking, Guid familiaId, DateOnly hoje)
    {
        if (new DateOnly(ranking.Ano, ranking.Mes, 1).AddMonths(1) > hoje)
        {
            return [];
        }

        return ranking.Top1
            .Select(top => new TopDoMes
            {
                FamiliaId = familiaId,
                Ano = ranking.Ano,
                Mes = ranking.Mes,
                MembroId = top.MembroId,
                Nome = top.Nome,
                Porcentagem = top.Porcentagem,
                Feitas = top.Feitas,
            })
            .ToList();
    }
}
