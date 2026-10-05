using RegraDeCasamento.Domain.Comum;
using RegraDeCasamento.Domain.Familias;

namespace RegraDeCasamento.Domain.Financas;

/// <summary>Os tipos de renda do R7.</summary>
public enum TipoDeRenda
{
    /// <summary>Ex.: salário no 5º dia útil.</summary>
    Fixa,

    /// <summary>Ex.: comissão. O valor é uma estimativa.</summary>
    Variavel,

    /// <summary>Ex.: um valor recebido por 4 meses. Tem data de fim.</summary>
    Temporaria,

    PensaoRecebida,

    /// <summary>Ex.: vale-alimentação, só para comida. Diz para que pode ser usado.</summary>
    BeneficioDeUsoRestrito,
}

/// <summary>Os dados para lançar uma renda.</summary>
public sealed record NovaRenda(
    string Descricao,
    decimal Valor,
    Guid? DonoId,
    TipoDeRenda Tipo,
    int DiaDeRecebimento,
    bool EmDiaUtil,
    DateOnly? DataDeFim,
    string? UsoRestrito);

/// <summary>Um dinheiro que a família recebe, com dono (R6), tipo e dia de recebimento (R7).</summary>
public sealed class Renda : Entidade, IPertenceAFamilia
{
    public const int TamanhoMaximoDaDescricao = 120;
    public const int TamanhoMaximoDoUso = 120;

    private Renda()
    {
    }

    public Guid FamiliaId { get; private set; }

    public string Descricao { get; private set; } = "";

    public decimal Valor { get; private set; }

    /// <summary>De quem é a renda: vazio = da casa; senão, o adulto dono (R6).</summary>
    public Guid? DonoId { get; private set; }

    public TipoDeRenda Tipo { get; private set; }

    /// <summary>O dia do mês (1 a 31) ou, com <see cref="EmDiaUtil"/>, o número do dia útil (ex.: 5º dia útil).</summary>
    public int DiaDeRecebimento { get; private set; }

    public bool EmDiaUtil { get; private set; }

    /// <summary>Só na renda temporária: o último dia em que ela vale.</summary>
    public DateOnly? DataDeFim { get; private set; }

    /// <summary>Só no benefício de uso restrito: para que ele pode ser usado (ex.: "comida").</summary>
    public string? UsoRestrito { get; private set; }

    public static ResultadoDeDominio<Renda> Lancar(Familia familia, Guid adultoId, NovaRenda dados)
    {
        var descricao = Validacao.TextoObrigatorio(dados.Descricao, TamanhoMaximoDaDescricao, "a descrição");
        var valor = Validacao.ValorPositivo(dados.Valor, "o valor");
        if (dados.DiaDeRecebimento is < 1 or > 31)
        {
            throw new DadosInvalidosException("O dia de recebimento precisa ser de 1 a 31.");
        }

        var falha = DonoDoDinheiro.ConferirQuemMexe(familia, adultoId)
            ?? DonoDoDinheiro.ConferirDono(familia, dados.DonoId)
            ?? DonoDoDinheiro.ConferirQuemAltera(adultoId, dados.DonoId);
        if (falha is not null)
        {
            return falha.RepassarFalha<Renda>();
        }

        if (dados.Tipo == TipoDeRenda.Temporaria && dados.DataDeFim is null)
        {
            return ResultadoDeDominio.Falha<Renda>("R7", "Renda temporária precisa da data de fim.");
        }

        if (dados.Tipo == TipoDeRenda.BeneficioDeUsoRestrito && string.IsNullOrWhiteSpace(dados.UsoRestrito))
        {
            return ResultadoDeDominio.Falha<Renda>("R7", "Benefício de uso restrito precisa dizer para que pode ser usado (ex.: comida).");
        }

        return ResultadoDeDominio.Ok(new Renda
        {
            FamiliaId = familia.Id,
            Descricao = descricao,
            Valor = valor,
            DonoId = dados.DonoId,
            Tipo = dados.Tipo,
            DiaDeRecebimento = dados.DiaDeRecebimento,
            EmDiaUtil = dados.EmDiaUtil,

            // Data de fim e uso só existem nos tipos que precisam deles.
            DataDeFim = dados.Tipo == TipoDeRenda.Temporaria ? dados.DataDeFim : null,
            UsoRestrito = dados.Tipo == TipoDeRenda.BeneficioDeUsoRestrito
                ? Validacao.TextoObrigatorio(dados.UsoRestrito, TamanhoMaximoDoUso, "o uso do benefício")
                : null,
        });
    }

    /// <summary>A renda ainda vale neste dia? Só a temporária acaba (R7).</summary>
    public bool ValeEm(DateOnly dia) => DataDeFim is null || dia <= DataDeFim.Value;
}
