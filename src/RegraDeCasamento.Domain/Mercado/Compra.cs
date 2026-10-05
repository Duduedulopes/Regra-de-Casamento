using RegraDeCasamento.Domain.Comum;
using RegraDeCasamento.Domain.Familias;

namespace RegraDeCasamento.Domain.Mercado;

/// <summary>Para que a compra foi feita (R12).</summary>
public enum FinalidadeDaCompra
{
    SuprimentoDaCasa,
    LancheParaTodos,
    Pessoal,
}

/// <summary>Um item da compra. O total é quantidade × valor unitário.</summary>
public sealed class ItemDaCompra
{
    public const int TamanhoMaximoDaDescricao = 120;

    private ItemDaCompra()
    {
    }

    /// <summary>Gerado no C#, como todo Id (STACK.md, regra 2).</summary>
    public Guid Id { get; private set; }

    public string Descricao { get; private set; } = "";

    /// <summary>Aceita decimais (ex.: 1,5 kg).</summary>
    public decimal Quantidade { get; private set; }

    public decimal ValorUnitario { get; private set; }

    public decimal Total => decimal.Round(Quantidade * ValorUnitario, 2, MidpointRounding.AwayFromZero);

    internal static ItemDaCompra Novo(NovoItem dados)
    {
        var quantidade = decimal.Round(dados.Quantidade, 3, MidpointRounding.AwayFromZero);
        if (quantidade <= 0)
        {
            throw new DadosInvalidosException("A quantidade precisa ser maior que zero.");
        }

        return new ItemDaCompra
        {
            Id = Guid.CreateVersion7(),
            Descricao = Validacao.TextoObrigatorio(dados.Descricao, TamanhoMaximoDaDescricao, "a descrição do item"),
            Quantidade = quantidade,
            ValorUnitario = Validacao.ValorPositivo(dados.ValorUnitario, "o valor do item"),
        };
    }
}

public sealed record NovoItem(string Descricao, decimal Quantidade, decimal ValorUnitario);

public sealed record NovaCompra(
    DateOnly Dia,
    string Local,
    FinalidadeDaCompra Finalidade,
    Guid? ParaQuemId,
    IReadOnlyList<NovoItem> Itens);

/// <summary>
/// Uma compra detalhada: o que foi comprado, quem comprou e para quê (R12). Fica no histórico e
/// alimenta a planilha de controle do casal (R13); por isso não se apaga.
/// </summary>
public sealed class Compra : Entidade, IPertenceAFamilia
{
    public const int TamanhoMaximoDoLocal = 80;

    private readonly List<ItemDaCompra> _itens = [];

    private Compra()
    {
    }

    public Guid FamiliaId { get; private set; }

    public DateOnly Dia { get; private set; }

    public string Local { get; private set; } = "";

    /// <summary>O adulto que comprou, que é sempre quem lançou a compra (R12, R37).</summary>
    public Guid CompradoPorId { get; private set; }

    public FinalidadeDaCompra Finalidade { get; private set; }

    /// <summary>Só na compra pessoal: o membro para quem ela foi feita.</summary>
    public Guid? ParaQuemId { get; private set; }

    public IReadOnlyCollection<ItemDaCompra> Itens => _itens.AsReadOnly();

    public decimal Total => _itens.Sum(item => item.Total);

    public static ResultadoDeDominio<Compra> Lancar(Familia familia, Guid adultoId, NovaCompra dados)
    {
        var local = Validacao.TextoObrigatorio(dados.Local, TamanhoMaximoDoLocal, "o local da compra");
        if (!Enum.IsDefined(dados.Finalidade))
        {
            throw new DadosInvalidosException("Escolha para que foi a compra.");
        }

        var quemLanca = familia.Membros.FirstOrDefault(m => m.Id == adultoId);
        if (quemLanca is null)
        {
            return ResultadoDeDominio.Falha<Compra>("R5", "Quem não é desta família não lança compras nela.");
        }

        if (quemLanca.Perfil != PerfilDoMembro.Adulto)
        {
            return ResultadoDeDominio.Falha<Compra>("R4", "As compras são só dos adultos.");
        }

        if (dados.Itens is null or { Count: 0 })
        {
            return ResultadoDeDominio.Falha<Compra>("R12", "A compra precisa ter pelo menos um item.");
        }

        if (dados.Finalidade == FinalidadeDaCompra.Pessoal
            && (dados.ParaQuemId is not { } paraQuem || familia.Membros.All(m => m.Id != paraQuem)))
        {
            return ResultadoDeDominio.Falha<Compra>("R12", "Compra pessoal precisa dizer para quem da família ela foi.");
        }

        var compra = new Compra
        {
            FamiliaId = familia.Id,
            Dia = dados.Dia,
            Local = local,
            // Quem lança é quem comprou: cada adulto registra as próprias compras (R12, R37).
            CompradoPorId = adultoId,
            Finalidade = dados.Finalidade,
            ParaQuemId = dados.Finalidade == FinalidadeDaCompra.Pessoal ? dados.ParaQuemId : null,
        };
        compra._itens.AddRange(dados.Itens.Select(ItemDaCompra.Novo));
        return ResultadoDeDominio.Ok(compra);
    }
}
