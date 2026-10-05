namespace RegraDeCasamento.Shared.Comum;

/// <summary>Resposta 422: uma regra de negócio não deixou (ex.: "R34").</summary>
public sealed record FalhaDeRegraDto(string CodigoDaRegra, string Mensagem);

/// <summary>Resposta 400: algum dado enviado não serve (vazio, grande demais, senha fraca).</summary>
public sealed record ErrosDeDadosDto(IReadOnlyList<string> Erros);
