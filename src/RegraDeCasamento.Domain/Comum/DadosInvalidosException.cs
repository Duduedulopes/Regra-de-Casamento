namespace RegraDeCasamento.Domain.Comum;

/// <summary>
/// Dado de entrada que não serve (vazio, grande demais, com caractere proibido). Não é regra de negócio:
/// a API devolve 400 com as mensagens, que já estão escritas para quem usa o app.
/// </summary>
public sealed class DadosInvalidosException : ArgumentException
{
    public DadosInvalidosException(string erro)
        : this([erro])
    {
    }

    public DadosInvalidosException(IReadOnlyList<string> erros)
        : base(string.Join(" ", erros))
    {
        Erros = erros;
    }

    public IReadOnlyList<string> Erros { get; }
}
