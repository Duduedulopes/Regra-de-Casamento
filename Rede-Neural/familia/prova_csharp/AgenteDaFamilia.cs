using System.Text.Json;
using System.Text.RegularExpressions;

namespace ProvaAgente;

public enum TipoDePerfil { Adulto, Crianca }

/// <summary>Quem fala com o agente. O agente é DESTE membro (R27).</summary>
/// <remarks>
/// LISTA DE PERMISSÃO TAMBÉM PARA O ADULTO. No `PerfilDeQuemFala` da loja, o Chefe
/// tem "conjunto vazio = tudo". Aqui não: intenção nova nasce fechada para os dois
/// perfis, e só abre quando alguém a põe no `perfis.json` de propósito.
/// </remarks>
public sealed record PerfilDeQuemFala(
    Guid MembroId, Guid FamiliaId, string Nome, TipoDePerfil Tipo, IReadOnlySet<string> Permitidas)
{
    public bool Pode(string intencao) => Permitidas.Contains(intencao);
}

/// <summary>As listas de cada perfil, lidas do `perfis.json` que sai junto com o modelo.</summary>
public sealed class Perfis(
    IReadOnlyDictionary<TipoDePerfil, IReadOnlySet<string>> listas,
    IReadOnlyDictionary<string, string> rotulos)
{
    /// <summary>O texto do botão. Sem rótulo, cai no nome técnico (e o Program avisa).</summary>
    public string Rotulo(string intencao) => rotulos.GetValueOrDefault(intencao, intencao);

    public IEnumerable<string> SemRotulo => listas.Values.SelectMany(l => l).Distinct().Where(i => !rotulos.ContainsKey(i));

    public static Perfis Carregar(string caminho)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(caminho));
        var p = doc.RootElement.GetProperty("perfis");
        IReadOnlySet<string> Lista(string nome) =>
            p.GetProperty(nome).EnumerateArray().Select(e => e.GetString()!).ToHashSet();
        var rotulos = doc.RootElement.TryGetProperty("rotulos", out var r)
            ? r.EnumerateObject().ToDictionary(o => o.Name, o => o.Value.GetString()!)
            : [];
        return new Perfis(new Dictionary<TipoDePerfil, IReadOnlySet<string>>
        {
            [TipoDePerfil.Adulto] = Lista("Adulto"),
            [TipoDePerfil.Crianca] = Lista("Crianca"),
        }, rotulos);
    }

    public PerfilDeQuemFala De(Guid membro, Guid familia, string nome, TipoDePerfil tipo) =>
        new(membro, familia, nome, tipo, listas[tipo]);
}

/// <summary>Os dados que a Application devolve para uma intenção, já filtrados por família (R5).</summary>
public interface IDadosDaFamilia
{
    /// <summary>Texto com os dados da intenção, ou null se não houver. NUNCA lê chat (R36).</summary>
    string? Buscar(PerfilDeQuemFala quem, string intencao);
}

/// <summary>A LLM. Só redige; não busca nem executa nada.</summary>
public interface IRedator
{
    string? Redigir(PerfilDeQuemFala quem, string pergunta, string intencao, string dados);
}

/// <summary>Um botão: o que a pessoa lê e a intenção que ele escolhe.</summary>
public sealed record Botao(string Intencao, string Texto);

/// <summary>A resposta do agente. `Botoes` vazio = só texto.</summary>
public sealed record Resposta(string Texto, IReadOnlyList<Botao> Botoes, string Origem);

/// <summary>
/// O agente pessoal de um membro: R4, R27, R28, R29 e R36 aplicadas no código.
/// </summary>
/// <remarks>
/// A REGRA DURA DE 05/10/2026. No teste da LLM, uma criança perguntou quanto a mãe
/// ganha. O contexto dizia BLOQUEADO, sem número nenhum, e a LLM respondeu
/// "a mãe ganha R$ 2.000,00". Então:
///
///     recusa  → texto fixo, a LLM nem é chamada
///     ação    → texto fixo + botão Confirmar (R29)
///     consulta → a LLM pode redigir, mas todo número dela tem que estar nos dados
/// </remarks>
public sealed class AgenteDaFamilia(
    ModeloDeIntencao modelo, Perfis perfis, IDadosDaFamilia dados, IRedator? redator, HistoricoDoAgente historico)
{
    private static readonly HashSet<string> Acoes =
    [
        "lancar_despesa", "lancar_renda", "marcar_conta_paga", "lancar_compra", "registrar_habito",
        "marcar_tarefa_feita", "pedir_troca", "responder_troca", "comecar_estudo", "terminar_estudo",
    ];

    private static readonly Dictionary<string, string> Fixas = new()
    {
        ["saudacao"] = "Oi, {nome}! Em que posso ajudar?",
        ["ajuda"] = "Posso mostrar suas tarefas, pontos, ranking e horário de estudo, e registrar o que você fizer.",
        ["agradecer"] = "Por nada, {nome}!",
        ["confirmar"] = "Não tem nada esperando confirmação agora.",
        ["cancelar"] = "Tudo bem, não fiz nada.",
        ["fora_do_escopo"] = "Isso eu não sei fazer, {nome}. Eu cuido das coisas da família, e não ensino matéria da escola.",
    };

    public Resposta Responder(PerfilDeQuemFala quem, string pergunta)
    {
        var resposta = Decidir(quem, pergunta);
        historico.Gravar(quem, pergunta, resposta.Texto);   // na conversa DESTE membro (R36)
        return resposta;
    }

    private Resposta Decidir(PerfilDeQuemFala quem, string pergunta)
    {
        var i = modelo.Classificar(pergunta);

        // 1. Abaixo do limiar: pergunta de volta com botões, só os que o perfil pode.
        if (!i.Confiavel)
        {
            var botoes = i.Tres.Where(quem.Pode).Select(n => new Botao(n, perfis.Rotulo(n))).ToList();
            return botoes.Count == 0
                ? Fixa(quem, "fora_do_escopo")
                : new Resposta($"Não tenho certeza, {quem.Nome}. Você quis dizer:", botoes, "botoes");
        }

        // 2. R4/R28: fora do perfil é recusado ANTES de qualquer busca de dado.
        if (!quem.Pode(i.Nome))
            return new Resposta($"Desculpe, {quem.Nome}, isso eu não posso te mostrar.", [], "recusa");

        if (Fixas.ContainsKey(i.Nome)) return Fixa(quem, i.Nome);

        var contexto = dados.Buscar(quem, i.Nome);

        // 3. R29: ação sempre pede confirmação, num botão do C#.
        if (Acoes.Contains(i.Nome))
            return new Resposta(contexto ?? $"Vou registrar: {i.Nome.Replace('_', ' ')}.",
                                [new("confirmar", perfis.Rotulo("confirmar")), new("cancelar", perfis.Rotulo("cancelar"))],
                                "acao");

        if (contexto is null)
            return new Resposta($"Ainda não tenho dados sobre isso, {quem.Nome}.", [], "sem_dados");

        // 4. Consulta: a LLM pode melhorar o texto, mas não pode inventar número.
        var texto = redator?.Redigir(quem, pergunta, i.Nome, contexto);
        if (texto is not null && GuardaDeNumeros.SoUsaNumerosDe(texto, contexto))
            return new Resposta(texto, [], "llm");
        return new Resposta(contexto, [], "fixa");
    }

    private static Resposta Fixa(PerfilDeQuemFala quem, string intencao) =>
        new(Fixas[intencao].Replace("{nome}", quem.Nome), [], "fixa");
}

/// <summary>Todo número que a LLM escreveu precisa estar nos dados que ela recebeu.</summary>
public static partial class GuardaDeNumeros
{
    /// <remarks>
    /// E O CONTRÁRIO TAMBÉM: se os dados têm número e a resposta não tem nenhum, ela
    /// jogou fora a informação. No teste de 05/10/2026, "quem tá ganhando o ranking?"
    /// com o ranking inteiro nos dados virou só "Lia.".
    /// </remarks>
    public static bool SoUsaNumerosDe(string texto, string dados)
    {
        var permitidos = Numeros(dados).ToHashSet();
        var usados = Numeros(texto).ToList();
        if (permitidos.Count > 0 && usados.Count == 0) return false;
        return usados.All(permitidos.Contains);
    }

    // "1.760", "1760", "R$ 1.760,00" e "1760,5" viram a mesma forma: só dígitos e vírgula decimal.
    private static IEnumerable<string> Numeros(string s) =>
        Numero().Matches(s).Select(m =>
        {
            var partes = m.Value.TrimEnd('.').Replace(".", "").Split(',');
            var dec = partes.Length > 1 ? partes[1].TrimEnd('0') : "";
            return dec.Length > 0 ? partes[0] + "," + dec : partes[0];
        });

    [GeneratedRegex(@"\d[\d.]*(,\d+)?")] private static partial Regex Numero();
}

/// <summary>
/// A conversa de cada membro com o próprio agente (R36). Na Web real, é uma tabela com
/// `FamiliaId` e `MembroId`, filtrada pelo membro do cookie.
/// </summary>
public sealed class HistoricoDoAgente
{
    private readonly Dictionary<Guid, List<(string Pergunta, string Resposta)>> _porMembro = [];

    public void Gravar(PerfilDeQuemFala dono, string pergunta, string resposta)
    {
        if (!_porMembro.TryGetValue(dono.MembroId, out var lista))
            _porMembro[dono.MembroId] = lista = [];
        lista.Add((pergunta, resposta));
    }

    /// <summary>Só o próprio dono lê. Nem um adulto lê a conversa da criança.</summary>
    public IReadOnlyList<(string Pergunta, string Resposta)> Ler(PerfilDeQuemFala quemPede, Guid dono)
    {
        if (quemPede.MembroId != dono)
            throw new UnauthorizedAccessException("R36: a conversa com o agente é só de quem fala.");
        return _porMembro.TryGetValue(dono, out var l) ? l : [];
    }
}
