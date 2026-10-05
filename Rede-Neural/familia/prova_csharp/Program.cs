// Prova do agente da família em C#. Os dados e as pessoas são inventados.
//
//     dotnet run                          os casos de adulto e criança
//     dotnet run -- --llm                 o mesmo, com o Qwen de verdade no lugar da LLM de mentira
//     dotnet run -- --paridade arq.json   confere o C# contra as respostas do Python

using System.Text.Json;
using ProvaAgente;

var pasta = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
var modelo = ModeloDeIntencao.Carregar(Path.Combine(pasta, "modelo_intencao.json"));
var perfis = Perfis.Carregar(Path.Combine(pasta, "perfis.json"));

if (args.Length == 2 && args[0] == "--paridade")
{
    // Cada linha: {"pergunta", "intencao", "confianca"} calculados pelo Python.
    var esperado = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(File.ReadAllText(args[1]))!;
    var diferentes = 0;
    var maiorDiferenca = 0.0;
    foreach (var e in esperado)
    {
        var i = modelo.Classificar(e["pergunta"].GetString()!);
        var dif = Math.Abs(i.Confianca - e["confianca"].GetDouble());
        maiorDiferenca = Math.Max(maiorDiferenca, dif);
        if (i.Nome != e["intencao"].GetString()) diferentes++;
    }
    Console.WriteLine($"paridade: {esperado.Count} frases, {diferentes} intenções diferentes, " +
                      $"maior diferença de confiança {maiorDiferenca:E1}");
    return diferentes == 0 ? 0 : 1;
}

var familia = Guid.NewGuid();
var carla = perfis.De(Guid.NewGuid(), familia, "Carla", TipoDePerfil.Adulto);
var pedro = perfis.De(Guid.NewGuid(), familia, "Pedro", TipoDePerfil.Crianca);
var lia = perfis.De(Guid.NewGuid(), familia, "Lia", TipoDePerfil.Crianca);

using var qwen = args.Contains("--llm")
    ? new RedatorQwen(Path.Combine(pasta, "llm", "qwen2.5-1.5b-instruct-q4_k_m.gguf"))
    : null;
var redator = new ContadorDeChamadas(qwen is null ? new RedatorQueInventa() : qwen);
var historico = new HistoricoDoAgente();
var agente = new AgenteDaFamilia(modelo, perfis, new DadosInventados(), redator, historico);

var semRotulo = perfis.SemRotulo.ToList();
if (semRotulo.Count > 0) Console.WriteLine($"AVISO: intenções sem rótulo de botão: {string.Join(", ", semRotulo)}\n");

foreach (var (quem, pergunta) in new[]
{
    (carla, "quanto cada um pagou esse mes"),
    (carla, "lancei 85 no mercado"),
    (pedro, "quanto a mae ganha"),
    (pedro, "qual o saldo da casa"),
    (lia, "quem ta ganhando o ranking"),
    (lia, "comecei a estudar"),
    (lia, "me ajuda na licao de matematica"),
    (pedro, "quantos pontos eu tenho"),
})
{
    var r = agente.Responder(quem, pergunta);
    var botoes = r.Botoes.Count > 0 ? $"  [{string.Join("] [", r.Botoes.Select(b => b.Texto))}]" : "";
    Console.WriteLine($"{quem.Nome} ({quem.Tipo}): \"{pergunta}\"");
    Console.WriteLine($"  {r.Origem,-8} {r.Texto}{botoes}\n");
}

Console.WriteLine($"a LLM foi chamada {redator.Chamadas} vez(es); nenhuma numa pergunta recusada");

// R36: a Carla (adulta) não lê a conversa da Lia com o agente dela.
try
{
    historico.Ler(carla, lia.MembroId);
    Console.WriteLine("ERRO: a adulta leu a conversa da criança");
    return 1;
}
catch (UnauthorizedAccessException e)
{
    Console.WriteLine($"R36 ok: {e.Message} (a Lia tem {historico.Ler(lia, lia.MembroId).Count} mensagens)");
}
return 0;

/// <summary>Dados de mentira, no formato que a Application devolveria.</summary>
sealed class DadosInventados : IDadosDaFamilia
{
    public string? Buscar(PerfilDeQuemFala quem, string intencao) => intencao switch
    {
        "ver_contribuicao" => "Em setembro, você pagou R$ 1.760 (55%) e o Marcos R$ 1.440 (45%) das contas da casa.",
        "ver_ranking" => "Ranking de outubro: Lia 92%, Pedro 85%, Carla 80%, Marcos 74%.",
        "ver_pontos" => "Você tem 23 pontos em outubro.",
        "lancar_compra" => "Vou lançar uma compra de mercado de R$ 85, como suprimento da casa.",
        "comecar_estudo" => "Vou marcar o início do seu estudo agora. Hoje o planejado é das 15h às 16h.",
        _ => null,
    };
}

/// <summary>Conta as chamadas à LLM, seja ela qual for, e mostra o que ela escreveu e quanto demorou.</summary>
sealed class ContadorDeChamadas(IRedator interno) : IRedator
{
    public int Chamadas { get; private set; }

    public string? Redigir(PerfilDeQuemFala quem, string pergunta, string intencao, string dados)
    {
        Chamadas++;
        var relogio = System.Diagnostics.Stopwatch.StartNew();
        var texto = interno.Redigir(quem, pergunta, intencao, dados);
        Console.WriteLine($"  (LLM, {relogio.Elapsed.TotalSeconds:0.0} s: {texto})");
        return texto;
    }
}

/// <summary>
/// Imita a LLM do teste de 05/10/2026: às vezes redige bem, às vezes inventa um número.
/// </summary>
sealed class RedatorQueInventa : IRedator
{
    public string? Redigir(PerfilDeQuemFala quem, string pergunta, string intencao, string dados)
    {
        return intencao == "ver_ranking"
            ? $"{quem.Nome}, você está em primeiro com 92%, e o Pedro vem logo atrás com 85%!"
            : $"{quem.Nome}, a conta deu uns R$ 2.000,00.";   // número inventado: a guarda tem que barrar
    }
}
