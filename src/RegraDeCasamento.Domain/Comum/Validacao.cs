namespace RegraDeCasamento.Domain.Comum;

/// <summary>Conferências de dado de entrada usadas por todo o Domain. Dado ruim lança <see cref="DadosInvalidosException"/>.</summary>
internal static class Validacao
{
    /// <summary>Texto preenchido, sem espaços nas pontas e dentro do tamanho. A descrição entra na mensagem (ex.: "o nome da família").</summary>
    public static string TextoObrigatorio(string? valor, int tamanhoMaximo, string descricao)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DadosInvalidosException($"Preencha {descricao}.");
        }

        var texto = valor.Trim();
        if (texto.Length > tamanhoMaximo)
        {
            throw new DadosInvalidosException($"{Maiuscula(descricao)} pode ter no máximo {tamanhoMaximo} caracteres.");
        }

        return texto;
    }

    /// <summary>Valor em reais maior que zero, guardado com 2 casas.</summary>
    public static decimal ValorPositivo(decimal valor, string descricao)
    {
        var arredondado = decimal.Round(valor, 2, MidpointRounding.AwayFromZero);
        if (arredondado <= 0)
        {
            throw new DadosInvalidosException($"{Maiuscula(descricao)} precisa ser maior que zero.");
        }

        return arredondado;
    }

    private static string Maiuscula(string texto) => $"{char.ToUpperInvariant(texto[0])}{texto[1..]}";
}
