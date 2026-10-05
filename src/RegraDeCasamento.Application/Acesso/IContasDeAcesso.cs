namespace RegraDeCasamento.Application.Acesso;

/// <summary>As contas de login, vistas pelos casos de uso. Quem implementa é a Infrastructure (Identity).</summary>
public interface IContasDeAcesso
{
    /// <summary>O membro ligado à conta, lido do banco (não do cookie, que pode estar desatualizado).</summary>
    Task<Guid?> ObterMembroIdAsync(Guid contaId, CancellationToken cancellationToken = default);

    /// <summary>Liga a conta ao membro. Só é gravado no <c>SalvarAsync</c> da unidade de trabalho.</summary>
    Task VincularAoMembroAsync(Guid contaId, Guid membroId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cria a conta de uma criança (usuário + PIN, R35) já ligada ao membro e grava na hora, junto com o que
    /// estiver pendente. PIN inválido ou usuário repetido lança <c>DadosInvalidosException</c>.
    /// </summary>
    Task CriarContaDeCriancaAsync(string usuario, string pin, Guid membroId, CancellationToken cancellationToken = default);
}
