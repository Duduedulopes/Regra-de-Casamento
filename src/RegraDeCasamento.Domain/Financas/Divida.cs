using RegraDeCasamento.Domain.Comum;
using RegraDeCasamento.Domain.Familias;

namespace RegraDeCasamento.Domain.Financas;

/// <summary>Os tipos de dívida do R38.</summary>
public enum TipoDeDivida
{
    CartaoDeCredito,
    Emprestimo,
    CompraParcelada,
}

public sealed record NovaDivida(
    string Descricao,
    TipoDeDivida Tipo,
    decimal ValorTotal,
    int NumeroDeParcelas,
    DateOnly PrimeiroVencimento,
    Guid? DonoId);

/// <summary>A dívida lançada e as parcelas, que já nascem como contas a pagar.</summary>
public sealed record DividaLancada(Divida Divida, IReadOnlyList<Despesa> Parcelas);

/// <summary>
/// Uma dívida com banco, loja ou cartão (R38): dono (R6), valor total e parcelas. Cada parcela é uma
/// <see cref="Despesa"/> a pagar no mês dela. Entre o casal não existe dívida (R9).
/// </summary>
public sealed class Divida : Entidade, IPertenceAFamilia
{
    public const int TamanhoMaximoDaDescricao = 100;
    public const int MaximoDeParcelas = 120;

    private Divida()
    {
    }

    public Guid FamiliaId { get; private set; }

    public string Descricao { get; private set; } = "";

    public TipoDeDivida Tipo { get; private set; }

    public decimal ValorTotal { get; private set; }

    public int NumeroDeParcelas { get; private set; }

    public DateOnly PrimeiroVencimento { get; private set; }

    /// <summary>De quem é a dívida: vazio = da casa; senão, o adulto dono (R6, R37).</summary>
    public Guid? DonoId { get; private set; }

    public static ResultadoDeDominio<DividaLancada> Lancar(Familia familia, Guid adultoId, NovaDivida dados)
    {
        var descricao = Validacao.TextoObrigatorio(dados.Descricao, TamanhoMaximoDaDescricao, "a descrição da dívida");
        var valorTotal = Validacao.ValorPositivo(dados.ValorTotal, "o valor total");
        if (dados.NumeroDeParcelas is < 1 or > MaximoDeParcelas)
        {
            throw new DadosInvalidosException($"O número de parcelas precisa ser de 1 a {MaximoDeParcelas}.");
        }

        if (!Enum.IsDefined(dados.Tipo))
        {
            throw new DadosInvalidosException("Escolha o tipo da dívida.");
        }

        if (valorTotal < dados.NumeroDeParcelas * 0.01m)
        {
            throw new DadosInvalidosException("O valor total é pequeno demais para tantas parcelas.");
        }

        var falha = DonoDoDinheiro.ConferirQuemMexe(familia, adultoId)
            ?? DonoDoDinheiro.ConferirDono(familia, dados.DonoId)
            ?? DonoDoDinheiro.ConferirQuemAltera(adultoId, dados.DonoId);
        if (falha is not null)
        {
            return falha.RepassarFalha<DividaLancada>();
        }

        var divida = new Divida
        {
            FamiliaId = familia.Id,
            Descricao = descricao,
            Tipo = dados.Tipo,
            ValorTotal = valorTotal,
            NumeroDeParcelas = dados.NumeroDeParcelas,
            PrimeiroVencimento = dados.PrimeiroVencimento,
            DonoId = dados.DonoId,
        };

        return ResultadoDeDominio.Ok(new DividaLancada(divida, divida.GerarParcelas()));
    }

    /// <summary>Uma parcela por mês, a partir do primeiro vencimento. A última leva os centavos que sobram da divisão.</summary>
    private List<Despesa> GerarParcelas()
    {
        var valorDaParcela = decimal.Round(ValorTotal / NumeroDeParcelas, 2, MidpointRounding.ToZero);
        var valorDaUltima = ValorTotal - (valorDaParcela * (NumeroDeParcelas - 1));

        return Enumerable.Range(1, NumeroDeParcelas)
            .Select(numero => Despesa.ParcelaDe(
                this,
                numero,
                numero == NumeroDeParcelas ? valorDaUltima : valorDaParcela,
                PrimeiroVencimento.AddMonths(numero - 1)))
            .ToList();
    }
}

/// <summary>Quanto da dívida já foi pago, contando as parcelas pagas (R38).</summary>
public sealed record AndamentoDaDivida(int ParcelasPagas, decimal ValorPago, decimal ValorQueFalta)
{
    public static AndamentoDaDivida Calcular(Divida divida, IEnumerable<Despesa> parcelas)
    {
        var pagas = parcelas.Where(p => p.DividaId == divida.Id && p.Situacao == SituacaoDaDespesa.Paga).ToList();
        var valorPago = pagas.Sum(p => p.Valor);
        return new AndamentoDaDivida(pagas.Count, valorPago, divida.ValorTotal - valorPago);
    }
}
