using System.Security.Cryptography;
using RegraDeCasamento.Domain.Comum;

namespace RegraDeCasamento.Domain.Familias;

/// <summary>
/// A família: o casal e os dependentes (R1). Toda entrada de membro passa por aqui, para que as regras
/// R1, R5, R34 e R35 sejam sempre conferidas.
/// </summary>
/// <remarks>
/// Texto vazio, longo demais ou com caractere proibido lança <see cref="DadosInvalidosException"/>
/// (a API responde 400). O que é regra de negócio volta como <see cref="ResultadoDeDominio"/>.
/// </remarks>
public sealed class Familia : Entidade
{
    /// <summary>O casal (R1). Decisão em aberto no SPEC.md: se a família puder ter mais adultos, muda só aqui.</summary>
    public const int MaximoDeAdultos = 2;

    public const int TamanhoMaximoDoNome = 100;
    public const int TamanhoDoCodigo = 8;

    // Sem letras e números que se confundem (I, L, O, 0, 1), porque o código é digitado por gente.
    private const string LetrasDoCodigo = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    private readonly List<Membro> _membros = [];
    private readonly List<PedidoDeEntrada> _pedidosDeEntrada = [];

    private Familia()
    {
    }

    public string Nome { get; private set; } = "";

    /// <summary>O código que um adulto usa para pedir entrada na família (R34).</summary>
    public string Codigo { get; private set; } = "";

    public IReadOnlyCollection<Membro> Membros => _membros.AsReadOnly();

    public IReadOnlyCollection<PedidoDeEntrada> PedidosDeEntrada => _pedidosDeEntrada.AsReadOnly();

    /// <summary>Cria a família com o adulto que a cadastrou. O outro adulto entra por pedido (R34).</summary>
    public static Familia Criar(string nome, string nomeDoAdulto)
    {
        var familia = new Familia
        {
            Nome = Validacao.TextoObrigatorio(nome, TamanhoMaximoDoNome, "o nome da família"),
            Codigo = RandomNumberGenerator.GetString(LetrasDoCodigo, TamanhoDoCodigo),
        };
        familia._membros.Add(Membro.NovoAdulto(familia.Id, Validacao.TextoObrigatorio(nomeDoAdulto, Membro.TamanhoMaximoDoNome, "o seu nome")));
        return familia;
    }

    /// <summary>O código como ele fica guardado: sem espaços nas pontas e em maiúsculas.</summary>
    public static string NormalizarCodigo(string codigo) => (codigo ?? "").Trim().ToUpperInvariant();

    /// <summary>Um adulto de fora pede para entrar. O pedido fica esperando a resposta de um adulto da família (R34).</summary>
    public ResultadoDeDominio<PedidoDeEntrada> ReceberPedidoDeEntrada(Guid contaDoSolicitanteId, string nomeDoSolicitante)
    {
        if (contaDoSolicitanteId == Guid.Empty)
        {
            throw new ArgumentException("A conta de quem pede não pode ser vazia.", nameof(contaDoSolicitanteId));
        }

        var nome = Validacao.TextoObrigatorio(nomeDoSolicitante, Membro.TamanhoMaximoDoNome, "o seu nome");

        if (EstaComOsAdultosCompletos())
        {
            return ResultadoDeDominio.Falha<PedidoDeEntrada>("R1", $"A família já tem os {MaximoDeAdultos} adultos.");
        }

        if (_pedidosDeEntrada.Exists(p => p.ContaDoSolicitanteId == contaDoSolicitanteId && p.Situacao == SituacaoDoPedido.Pendente))
        {
            return ResultadoDeDominio.Falha<PedidoDeEntrada>("R34", "Já existe um pedido desta conta esperando resposta.");
        }

        var pedido = PedidoDeEntrada.Novo(Id, contaDoSolicitanteId, nome);
        _pedidosDeEntrada.Add(pedido);
        return ResultadoDeDominio.Ok(pedido);
    }

    /// <summary>Um adulto da família aprova o pedido, e quem pediu entra como adulto (R34, R1).</summary>
    public ResultadoDeDominio<Membro> AprovarPedidoDeEntrada(Guid pedidoId, Guid adultoId)
    {
        var conferencia = PedidoQuePodeSerRespondido(pedidoId, adultoId);
        if (conferencia.Falhou)
        {
            return conferencia.RepassarFalha<Membro>();
        }

        if (EstaComOsAdultosCompletos())
        {
            return ResultadoDeDominio.Falha<Membro>("R1", $"A família já tem os {MaximoDeAdultos} adultos.");
        }

        var pedido = conferencia.Valor;
        pedido.Aprovar(adultoId);

        var novoAdulto = Membro.NovoAdulto(Id, pedido.NomeDoSolicitante);
        _membros.Add(novoAdulto);
        RegistrarAtualizacao();
        return ResultadoDeDominio.Ok(novoAdulto);
    }

    /// <summary>Um adulto da família recusa o pedido, e ninguém entra (R34).</summary>
    public ResultadoDeDominio RecusarPedidoDeEntrada(Guid pedidoId, Guid adultoId)
    {
        var conferencia = PedidoQuePodeSerRespondido(pedidoId, adultoId);
        if (conferencia.Falhou)
        {
            return conferencia;
        }

        conferencia.Valor.Recusar(adultoId);
        RegistrarAtualizacao();
        return ResultadoDeDominio.Ok();
    }

    /// <summary>Um adulto da família cria a conta de uma criança, que é identificada pelo apelido (R35).</summary>
    public ResultadoDeDominio<Membro> AdicionarCrianca(Guid adultoId, string nome, string apelido)
    {
        var nomeDaCrianca = Validacao.TextoObrigatorio(nome, Membro.TamanhoMaximoDoNome, "o nome da criança");
        var apelidoDaCrianca = Validacao.TextoObrigatorio(apelido, Membro.TamanhoMaximoDoApelido, "o apelido");
        if (!apelidoDaCrianca.All(letra => char.IsLetterOrDigit(letra) || letra is '.' or '-' or '_'))
        {
            // O apelido vira parte do login da criança, por isso não pode ter espaço nem símbolo.
            throw new DadosInvalidosException("O apelido só pode ter letras, números, ponto, hífen e sublinhado, sem espaços.");
        }

        var quemCria = _membros.Find(m => m.Id == adultoId);
        if (quemCria is null)
        {
            return ResultadoDeDominio.Falha<Membro>("R5", "Quem não é desta família não cria contas nela.");
        }

        if (quemCria.Perfil != PerfilDoMembro.Adulto)
        {
            return ResultadoDeDominio.Falha<Membro>("R35", "Só um adulto da família cria a conta de uma criança.");
        }

        if (_membros.Exists(m => string.Equals(m.Apelido, apelidoDaCrianca, StringComparison.OrdinalIgnoreCase)))
        {
            return ResultadoDeDominio.Falha<Membro>("R35", $"Já existe alguém com o apelido \"{apelidoDaCrianca}\" na família.");
        }

        var crianca = Membro.NovaCrianca(Id, nomeDaCrianca, apelidoDaCrianca);
        _membros.Add(crianca);
        RegistrarAtualizacao();
        return ResultadoDeDominio.Ok(crianca);
    }

    private ResultadoDeDominio<PedidoDeEntrada> PedidoQuePodeSerRespondido(Guid pedidoId, Guid adultoId)
    {
        var quemResponde = _membros.Find(m => m.Id == adultoId);
        if (quemResponde is null)
        {
            return ResultadoDeDominio.Falha<PedidoDeEntrada>("R5", "Quem não é desta família não responde pedidos dela.");
        }

        if (quemResponde.Perfil != PerfilDoMembro.Adulto)
        {
            return ResultadoDeDominio.Falha<PedidoDeEntrada>("R34", "Só um adulto da família responde um pedido de entrada.");
        }

        var pedido = _pedidosDeEntrada.Find(p => p.Id == pedidoId);
        if (pedido is null)
        {
            return ResultadoDeDominio.Falha<PedidoDeEntrada>("R5", "Este pedido não é desta família.");
        }

        if (pedido.Situacao != SituacaoDoPedido.Pendente)
        {
            return ResultadoDeDominio.Falha<PedidoDeEntrada>("R34", "Este pedido já foi respondido.");
        }

        return ResultadoDeDominio.Ok(pedido);
    }

    private bool EstaComOsAdultosCompletos() =>
        _membros.Count(m => m.Perfil == PerfilDoMembro.Adulto) >= MaximoDeAdultos;
}
