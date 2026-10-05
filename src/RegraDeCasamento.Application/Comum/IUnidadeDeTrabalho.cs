namespace RegraDeCasamento.Application.Comum;

/// <summary>Grava de uma vez tudo o que o caso de uso mudou: ou tudo entra no banco, ou nada entra.</summary>
public interface IUnidadeDeTrabalho
{
    Task SalvarAsync(CancellationToken cancellationToken = default);
}
