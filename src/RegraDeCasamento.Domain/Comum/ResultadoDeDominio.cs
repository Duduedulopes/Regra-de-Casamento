namespace RegraDeCasamento.Domain.Comum;

/// <summary>
/// Resposta de uma regra de negócio. Quando a regra não deixa, volta o código dela (ex.: "R18")
/// e uma mensagem. Regra de negócio nunca lança exceção.
/// </summary>
public class ResultadoDeDominio
{
    protected ResultadoDeDominio(string? codigoDaRegra, string? mensagem)
    {
        CodigoDaRegra = codigoDaRegra;
        Mensagem = mensagem;
    }

    public bool Sucesso => CodigoDaRegra is null;

    public bool Falhou => !Sucesso;

    /// <summary>A regra que não deixou (ex.: "R18"). Vazio quando deu certo.</summary>
    public string? CodigoDaRegra { get; }

    public string? Mensagem { get; }

    public static ResultadoDeDominio Ok() => new(null, null);

    public static ResultadoDeDominio<T> Ok<T>(T valor) => new(valor, null, null);

    public static ResultadoDeDominio Falha(string codigoDaRegra, string mensagem)
    {
        ValidarFalha(codigoDaRegra, mensagem);
        return new(codigoDaRegra, mensagem);
    }

    public static ResultadoDeDominio<T> Falha<T>(string codigoDaRegra, string mensagem)
    {
        ValidarFalha(codigoDaRegra, mensagem);
        return new(default, codigoDaRegra, mensagem);
    }

    /// <summary>Passa esta falha adiante, como resultado de outro tipo, com o mesmo código e a mesma mensagem.</summary>
    public ResultadoDeDominio<T> RepassarFalha<T>() =>
        Falhou
            ? new(default, CodigoDaRegra, Mensagem)
            : throw new InvalidOperationException("Só um resultado que falhou pode ser repassado como falha.");

    // Código errado ou mensagem vazia é erro de programação, não regra de negócio: por isso aqui é exceção.
    private static void ValidarFalha(string codigoDaRegra, string mensagem)
    {
        if (!EhCodigoDeRegra(codigoDaRegra))
        {
            throw new ArgumentException(
                $"O código da regra precisa ser R seguido de números (ex.: R18), mas veio \"{codigoDaRegra}\".",
                nameof(codigoDaRegra));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(mensagem);
    }

    private static bool EhCodigoDeRegra(string? codigo) =>
        codigo is { Length: > 1 }
        && codigo[0] == 'R'
        && codigo.AsSpan(1).IndexOfAnyExceptInRange('0', '9') < 0;
}

/// <summary>Resultado de uma regra que, quando deixa, devolve um valor (ex.: o membro que entrou na família).</summary>
public sealed class ResultadoDeDominio<T> : ResultadoDeDominio
{
    private readonly T? _valor;

    internal ResultadoDeDominio(T? valor, string? codigoDaRegra, string? mensagem)
        : base(codigoDaRegra, mensagem)
    {
        _valor = valor;
    }

    public T Valor =>
        Sucesso
            ? _valor!
            : throw new InvalidOperationException($"Não há valor: a regra {CodigoDaRegra} não deixou. {Mensagem}");
}
