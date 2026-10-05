using RegraDeCasamento.Domain.Comum;
using RegraDeCasamento.Domain.Familias;

namespace RegraDeCasamento.Domain.Tarefas;

public enum SituacaoDaTarefa
{
    FaltaFazer,
    Feita,
}

/// <summary>
/// Uma tarefa do checklist do dia, com um responsável (R16). Cumprida, vale 1 ponto para ele (R17).
/// Tarefas ficam separadas do dinheiro: ponto nunca vira dinheiro (R22).
/// </summary>
public sealed class Tarefa : Entidade, IPertenceAFamilia
{
    public const int TamanhoMaximoDoTitulo = 100;

    private Tarefa()
    {
    }

    public Guid FamiliaId { get; private set; }

    public string Titulo { get; private set; } = "";

    /// <summary>O dia do checklist em que a tarefa entra (R16).</summary>
    public DateOnly Dia { get; private set; }

    /// <summary>Quem faz. Muda só por troca aceita (R18).</summary>
    public Guid ResponsavelId { get; private set; }

    public SituacaoDaTarefa Situacao { get; private set; }

    public DateTime? FeitaEmUtc { get; private set; }

    /// <summary>Um adulto cria a tarefa e escolhe o responsável, que pode ser qualquer membro da família (R16).</summary>
    public static ResultadoDeDominio<Tarefa> Criar(Familia familia, Guid adultoId, string titulo, DateOnly dia, Guid responsavelId)
    {
        var texto = Validacao.TextoObrigatorio(titulo, TamanhoMaximoDoTitulo, "o nome da tarefa");

        var quemCria = familia.Membros.FirstOrDefault(m => m.Id == adultoId);
        if (quemCria is null)
        {
            return ResultadoDeDominio.Falha<Tarefa>("R5", "Quem não é desta família não cria tarefas nela.");
        }

        if (quemCria.Perfil != PerfilDoMembro.Adulto)
        {
            return ResultadoDeDominio.Falha<Tarefa>("R16", "Só um adulto cria e distribui as tarefas.");
        }

        if (familia.Membros.All(m => m.Id != responsavelId))
        {
            return ResultadoDeDominio.Falha<Tarefa>("R16", "O responsável precisa ser alguém da família.");
        }

        return ResultadoDeDominio.Ok(new Tarefa
        {
            FamiliaId = familia.Id,
            Titulo = texto,
            Dia = dia,
            ResponsavelId = responsavelId,
            Situacao = SituacaoDaTarefa.FaltaFazer,
        });
    }

    /// <summary>O responsável (ou um adulto) marca a tarefa como feita (R16). Ela passa a valer 1 ponto (R17).</summary>
    public ResultadoDeDominio MarcarComoFeita(Familia familia, Guid quemMarcaId)
    {
        var falha = ConferirQuemMarca(familia, quemMarcaId);
        if (falha is not null)
        {
            return falha;
        }

        if (Situacao == SituacaoDaTarefa.Feita)
        {
            return ResultadoDeDominio.Falha("R16", "Esta tarefa já está feita.");
        }

        Situacao = SituacaoDaTarefa.Feita;
        FeitaEmUtc = DateTime.UtcNow;
        RegistrarAtualizacao();
        return ResultadoDeDominio.Ok();
    }

    /// <summary>Volta para "falta fazer" (ex.: marcada por engano).</summary>
    public ResultadoDeDominio MarcarComoFaltaFazer(Familia familia, Guid quemMarcaId)
    {
        var falha = ConferirQuemMarca(familia, quemMarcaId);
        if (falha is not null)
        {
            return falha;
        }

        if (Situacao == SituacaoDaTarefa.FaltaFazer)
        {
            return ResultadoDeDominio.Falha("R16", "Esta tarefa já está como falta fazer.");
        }

        Situacao = SituacaoDaTarefa.FaltaFazer;
        FeitaEmUtc = null;
        RegistrarAtualizacao();
        return ResultadoDeDominio.Ok();
    }

    internal void PassarPara(Guid novoResponsavelId)
    {
        ResponsavelId = novoResponsavelId;
        RegistrarAtualizacao();
    }

    private ResultadoDeDominio? ConferirQuemMarca(Familia familia, Guid quemMarcaId)
    {
        if (familia.Id != FamiliaId)
        {
            return ResultadoDeDominio.Falha("R5", "Esta tarefa não é desta família.");
        }

        var quem = familia.Membros.FirstOrDefault(m => m.Id == quemMarcaId);
        if (quem is null)
        {
            return ResultadoDeDominio.Falha("R5", "Quem não é desta família não mexe nas tarefas dela.");
        }

        return quem.Id == ResponsavelId || quem.Perfil == PerfilDoMembro.Adulto
            ? null
            : ResultadoDeDominio.Falha("R16", "Só o responsável (ou um adulto) marca esta tarefa.");
    }
}
