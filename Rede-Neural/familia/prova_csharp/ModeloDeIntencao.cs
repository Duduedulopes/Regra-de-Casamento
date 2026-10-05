using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ProvaAgente;

/// <summary>A intenção que a rede achou e quanta certeza ela tem.</summary>
public sealed record Intencao(string Nome, double Confianca, IReadOnlyList<string> Tres, double Limiar)
{
    public bool Confiavel => Confianca >= Limiar;
}

/// <summary>
/// O classificador treinado no Python (`programas/treinar_familia.py`), rodando em C# puro.
/// </summary>
/// <remarks>
/// É a mesma conta do `rede/classificador.py`, linha por linha:
///
///     peças da frase → média dos vetores → oculta (sigmoid) → softmax
///
/// O `Program` confere que as duas línguas dão a mesma resposta para as mesmas frases.
/// Se um dia divergirem, o erro está aqui, porque o Python é onde o modelo foi medido.
/// </remarks>
public sealed partial class ModeloDeIntencao
{
    private readonly string[] _intencoes;
    private readonly Dictionary<string, int> _indice;
    private readonly double[][] _tabela;
    private readonly (string Ativacao, double[][] Pesos, double[] Vies)[] _camadas;

    public double Limiar { get; }
    public IReadOnlyList<string> Intencoes => _intencoes;

    private ModeloDeIntencao(JsonElement raiz)
    {
        _intencoes = raiz.GetProperty("intencoes").EnumerateArray().Select(e => e.GetString()!).ToArray();
        var pecas = raiz.GetProperty("pecas").EnumerateArray().Select(e => e.GetString()!).ToArray();
        _indice = pecas.Select((p, i) => (p, i)).ToDictionary(t => t.p, t => t.i);
        _tabela = Matriz(raiz.GetProperty("tabela"));
        _camadas = raiz.GetProperty("camadas").EnumerateArray()
            .Select(c => (c.GetProperty("ativacao").GetString()!,
                          Matriz(c.GetProperty("pesos")),
                          c.GetProperty("vies").EnumerateArray().Select(v => v.GetDouble()).ToArray()))
            .ToArray();
        Limiar = raiz.GetProperty("limiar").GetDouble();
    }

    public static ModeloDeIntencao Carregar(string caminho)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(caminho));
        return new ModeloDeIntencao(doc.RootElement);
    }

    public Intencao Classificar(string pergunta)
    {
        var indices = Pedacos(pergunta).Where(_indice.ContainsKey).Select(p => _indice[p]).ToList();
        if (indices.Count == 0) indices.Add(0);   // "<?>": responde pelo viés, como no Python

        var x = new double[_tabela[0].Length];
        foreach (var i in indices)
            for (var j = 0; j < x.Length; j++) x[j] += _tabela[i][j];
        for (var j = 0; j < x.Length; j++) x[j] /= indices.Count;

        foreach (var (ativacao, pesos, vies) in _camadas)
        {
            var z = new double[vies.Length];
            for (var i = 0; i < z.Length; i++)
            {
                var soma = vies[i];
                for (var j = 0; j < x.Length; j++) soma += pesos[i][j] * x[j];
                z[i] = soma;
            }
            x = ativacao switch
            {
                "sigmoid" => z.Select(v => 1.0 / (1.0 + Math.Exp(-v))).ToArray(),
                "softmax" => Softmax(z),
                _ => throw new InvalidDataException($"ativação desconhecida: {ativacao}"),
            };
        }

        var ordem = Enumerable.Range(0, x.Length).OrderByDescending(i => x[i]).ToArray();
        return new Intencao(_intencoes[ordem[0]], x[ordem[0]],
                            ordem.Take(3).Select(i => _intencoes[i]).ToArray(), Limiar);
    }

    // ── texto → peças: cópia do rede/texto.py ─────────────────────────────

    public static string Normalizar(string texto)
    {
        var t = (texto ?? "").ToLowerInvariant().Normalize(NormalizationForm.FormD);
        t = new string(t.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
        t = NaoAlfanumerico().Replace(t, " ");
        return Espacos().Replace(t, " ").Trim();
    }

    public static IEnumerable<string> Pedacos(string texto)
    {
        var palavras = Normalizar(texto).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in palavras) yield return p;
        foreach (var p in palavras)
        {
            var cercada = "<" + p + ">";
            if (cercada.Length <= 3) { yield return cercada; continue; }
            for (var i = 0; i + 3 <= cercada.Length; i++) yield return cercada.Substring(i, 3);
        }
    }

    private static double[] Softmax(double[] z)
    {
        var max = z.Max();
        var e = z.Select(v => Math.Exp(v - max)).ToArray();
        var soma = e.Sum();
        return e.Select(v => v / soma).ToArray();
    }

    private static double[][] Matriz(JsonElement e) =>
        e.EnumerateArray().Select(l => l.EnumerateArray().Select(v => v.GetDouble()).ToArray()).ToArray();

    [GeneratedRegex(@"[^a-z0-9\s]")] private static partial Regex NaoAlfanumerico();
    [GeneratedRegex(@"\s+")] private static partial Regex Espacos();
}
