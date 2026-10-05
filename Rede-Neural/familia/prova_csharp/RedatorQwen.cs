using System.Diagnostics;
using System.Text;
using LLama;
using LLama.Common;
using LLama.Native;
using LLama.Sampling;

namespace ProvaAgente;

/// <summary>
/// A LLM de verdade (Qwen2.5 1,5B, arquivo .gguf) dentro do C#, pelo LLamaSharp.
/// </summary>
/// <remarks>
/// SÓ É CHAMADA EM CONSULTA. O `AgenteDaFamilia` já resolveu recusa, ação e conversa
/// com texto fixo antes de chegar aqui, e confere os números do que ela devolver.
///
/// STATELESS DE PROPÓSITO. Cada pergunta começa do zero, com o contexto montado pelo C#.
/// Nada da conversa de um membro fica na memória do modelo para aparecer na de outro (R36).
/// </remarks>
public sealed class RedatorQwen : IRedator, IDisposable
{
    private const string Sistema =
        "Você é o agente pessoal de um membro de uma família, no app Regra de Casamento. " +
        "Responda em português do Brasil, gentil, em no máximo 2 frases curtas, sem saudação. " +
        "Fale direto com quem perguntou, pelo nome. Use SOMENTE os dados fornecidos e copie os números " +
        "exatamente como estão. Nunca invente números.";

    private readonly LLamaWeights _pesos;
    private readonly StatelessExecutor _executor;

    public TimeSpan UltimaDuracao { get; private set; }

    public RedatorQwen(string caminhoGguf, int threads = 4)
    {
        // O llama.cpp escreve dezenas de linhas de diagnóstico a cada pergunta. Só os erros interessam.
        NativeLogConfig.llama_log_set((nivel, mensagem) =>
        {
            if (nivel == LLama.Native.LLamaLogLevel.Error) Console.Error.Write(mensagem);
        });

        var parametros = new ModelParams(caminhoGguf) { ContextSize = 2048, Threads = threads };
        _pesos = LLamaWeights.LoadFromFile(parametros);
        _executor = new StatelessExecutor(_pesos, parametros);
    }

    public string? Redigir(PerfilDeQuemFala quem, string pergunta, string intencao, string dados)
    {
        var quemE = quem.Tipo == TipoDePerfil.Crianca ? "criança da família" : "adulto da família";
        var prompt =
            $"<|im_start|>system\n{Sistema}<|im_end|>\n" +
            $"<|im_start|>user\nQuem pergunta: {quem.Nome} ({quemE})\n" +
            $"Dados: {dados}\nPergunta: {pergunta}<|im_end|>\n" +
            "<|im_start|>assistant\n";

        var parametros = new InferenceParams
        {
            MaxTokens = 60,
            AntiPrompts = ["<|im_end|>"],
            SamplingPipeline = new DefaultSamplingPipeline { Temperature = 0.3f },
        };

        var relogio = Stopwatch.StartNew();
        var texto = new StringBuilder();
        foreach (var pedaco in _executor.InferAsync(prompt, parametros).ToBlockingEnumerable())
            texto.Append(pedaco);
        UltimaDuracao = relogio.Elapsed;

        var resposta = texto.ToString().Replace("<|im_end|>", "").Trim();
        return resposta.Length == 0 ? null : resposta;
    }

    public void Dispose() => _pesos.Dispose();
}
