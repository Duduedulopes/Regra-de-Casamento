namespace RegraDeCasamento.Domain.Comum;

/// <summary>
/// Base de toda entidade: o Id nasce no C# (nunca no banco) e as datas ficam sempre em UTC.
/// </summary>
public abstract class Entidade
{
    protected Entidade()
    {
        // Versão 7: o Guid carrega o momento da criação, então os registros novos entram em ordem no índice do banco.
        Id = Guid.CreateVersion7();
        CriadoEmUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public DateTime CriadoEmUtc { get; private set; }

    public DateTime? AtualizadoEmUtc { get; private set; }

    protected void RegistrarAtualizacao() => AtualizadoEmUtc = DateTime.UtcNow;
}
