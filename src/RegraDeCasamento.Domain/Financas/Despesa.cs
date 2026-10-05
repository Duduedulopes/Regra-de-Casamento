using RegraDeCasamento.Domain.Comum;
using RegraDeCasamento.Domain.Familias;

namespace RegraDeCasamento.Domain.Financas;

public enum SituacaoDaDespesa
{
    APagar,
    Paga,
}

/// <summary>
/// Uma conta ou gasto da família. Tem dono (a casa ou um adulto, R6), fica a pagar ou paga (R10)
/// e, depois de paga, guarda quem pagou e quando (R8).
/// </summary>
public sealed class Despesa : Entidade, IPertenceAFamilia
{
    public const int TamanhoMaximoDaDescricao = 120;

    private Despesa()
    {
    }

    public Guid FamiliaId { get; private set; }

    public string Descricao { get; private set; } = "";

    public decimal Valor { get; private set; }

    public DateOnly Vencimento { get; private set; }

    /// <summary>De quem é a despesa: vazio = da casa; senão, o adulto dono (R6).</summary>
    public Guid? DonoId { get; private set; }

    public SituacaoDaDespesa Situacao { get; private set; }

    /// <summary>O adulto que pagou (R8). Vazio enquanto está a pagar.</summary>
    public Guid? PagaPorId { get; private set; }

    public DateOnly? PagaEm { get; private set; }

    /// <summary>Quando a conta é a parcela de uma dívida (R38): a dívida e o número da parcela.</summary>
    public Guid? DividaId { get; private set; }

    public int? NumeroDaParcela { get; private set; }

    public bool EhDaCasa => DonoId is null;

    public static ResultadoDeDominio<Despesa> Lancar(
        Familia familia,
        Guid adultoId,
        string descricao,
        decimal valor,
        DateOnly vencimento,
        Guid? donoId)
    {
        var texto = Validacao.TextoObrigatorio(descricao, TamanhoMaximoDaDescricao, "a descrição");
        var valorEmReais = Validacao.ValorPositivo(valor, "o valor");

        var falha = DonoDoDinheiro.ConferirQuemMexe(familia, adultoId)
            ?? DonoDoDinheiro.ConferirDono(familia, donoId)
            ?? DonoDoDinheiro.ConferirQuemAltera(adultoId, donoId);
        if (falha is not null)
        {
            return falha.RepassarFalha<Despesa>();
        }

        return ResultadoDeDominio.Ok(Nova(familia.Id, texto, valorEmReais, vencimento, donoId));
    }

    /// <summary>A parcela de uma dívida, que vira uma conta a pagar no mês dela (R38).</summary>
    internal static Despesa ParcelaDe(Divida divida, int numero, decimal valor, DateOnly vencimento)
    {
        var parcela = Nova(divida.FamiliaId, $"{divida.Descricao} ({numero}/{divida.NumeroDeParcelas})", valor, vencimento, divida.DonoId);
        parcela.DividaId = divida.Id;
        parcela.NumeroDaParcela = numero;
        return parcela;
    }

    private static Despesa Nova(Guid familiaId, string descricao, decimal valor, DateOnly vencimento, Guid? donoId) => new()
    {
        FamiliaId = familiaId,
        Descricao = descricao,
        Valor = valor,
        Vencimento = vencimento,
        DonoId = donoId,
        Situacao = SituacaoDaDespesa.APagar,
    };

    /// <summary>Registra que a conta foi paga, por qual adulto e em que dia (R8, R10).</summary>
    public ResultadoDeDominio MarcarComoPaga(Familia familia, Guid adultoId, Guid pagaPorId, DateOnly pagaEm)
    {
        var falha = ConferirFamilia(familia, adultoId);
        if (falha is not null)
        {
            return falha;
        }

        if (!DonoDoDinheiro.EhAdultoDaFamilia(familia, pagaPorId))
        {
            return ResultadoDeDominio.Falha("R8", "Quem pagou precisa ser um dos adultos da família.");
        }

        if (Situacao == SituacaoDaDespesa.Paga)
        {
            return ResultadoDeDominio.Falha("R10", "Esta conta já está paga.");
        }

        Situacao = SituacaoDaDespesa.Paga;
        PagaPorId = pagaPorId;
        PagaEm = pagaEm;
        RegistrarAtualizacao();
        return ResultadoDeDominio.Ok();
    }

    /// <summary>Volta a conta para "a pagar" (ex.: foi marcada como paga por engano) (R10).</summary>
    public ResultadoDeDominio MarcarComoAPagar(Familia familia, Guid adultoId)
    {
        var falha = ConferirFamilia(familia, adultoId);
        if (falha is not null)
        {
            return falha;
        }

        if (Situacao == SituacaoDaDespesa.APagar)
        {
            return ResultadoDeDominio.Falha("R10", "Esta conta já está a pagar.");
        }

        Situacao = SituacaoDaDespesa.APagar;
        PagaPorId = null;
        PagaEm = null;
        RegistrarAtualizacao();
        return ResultadoDeDominio.Ok();
    }

    private ResultadoDeDominio? ConferirFamilia(Familia familia, Guid adultoId) =>
        familia.Id != FamiliaId
            ? ResultadoDeDominio.Falha("R5", "Esta conta não é desta família.")
            : DonoDoDinheiro.ConferirQuemMexe(familia, adultoId) ?? DonoDoDinheiro.ConferirQuemAltera(adultoId, DonoId);
}
