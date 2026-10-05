using System.Text.Json.Serialization;

namespace RegraDeCasamento.Shared.Financas;

[JsonConverter(typeof(JsonStringEnumConverter<SituacaoDeConta>))]
public enum SituacaoDeConta
{
    APagar,
    Paga,
}

/// <summary>Os tipos de renda do R7.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TipoDeRendaDto>))]
public enum TipoDeRendaDto
{
    Fixa,
    Variavel,
    Temporaria,
    PensaoRecebida,
    BeneficioDeUsoRestrito,
}

/// <summary>Uma conta nova. Sem <see cref="DonoId"/>, ela é da casa (R6).</summary>
public sealed record DadosDaNovaDespesa(string Descricao, decimal Valor, DateOnly Vencimento, Guid? DonoId);

/// <summary>Quem pagou (um adulto da família) e em que dia (R8).</summary>
public sealed record DadosDoPagamento(Guid PagaPorId, DateOnly PagaEm);

public sealed record DespesaDto(
    Guid Id,
    string Descricao,
    decimal Valor,
    DateOnly Vencimento,
    Guid? DonoId,
    string Dono,
    SituacaoDeConta Situacao,
    Guid? PagaPorId,
    string? PagaPor,
    DateOnly? PagaEm,
    Guid? DividaId);

/// <summary>Os tipos de dívida do R38.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TipoDeDividaDto>))]
public enum TipoDeDividaDto
{
    CartaoDeCredito,
    Emprestimo,
    CompraParcelada,
}

/// <summary>Uma dívida nova (R38). Cada parcela vira uma conta a pagar no mês dela.</summary>
public sealed record DadosDaNovaDivida(
    string Descricao,
    TipoDeDividaDto Tipo,
    decimal ValorTotal,
    int NumeroDeParcelas,
    DateOnly PrimeiroVencimento,
    Guid? DonoId);

public sealed record DividaDto(
    Guid Id,
    string Descricao,
    TipoDeDividaDto Tipo,
    decimal ValorTotal,
    int NumeroDeParcelas,
    DateOnly PrimeiroVencimento,
    Guid? DonoId,
    string Dono,
    int ParcelasPagas,
    decimal ValorPago,
    decimal ValorQueFalta);

public sealed record DadosDaNovaRenda(
    string Descricao,
    decimal Valor,
    Guid? DonoId,
    TipoDeRendaDto Tipo,
    int DiaDeRecebimento,
    bool EmDiaUtil,
    DateOnly? DataDeFim,
    string? UsoRestrito);

public sealed record RendaDto(
    Guid Id,
    string Descricao,
    decimal Valor,
    Guid? DonoId,
    string Dono,
    TipoDeRendaDto Tipo,
    int DiaDeRecebimento,
    bool EmDiaUtil,
    DateOnly? DataDeFim,
    string? UsoRestrito);

public sealed record ContribuicaoDoAdultoDto(Guid MembroId, string Nome, decimal Valor, decimal Porcentagem);

/// <summary>Quanto cada adulto pagou das contas da casa no mês (R9). Só registro, nunca cobrança.</summary>
public sealed record ContribuicaoDoMesDto(int Ano, int Mes, decimal TotalDaCasa, IReadOnlyList<ContribuicaoDoAdultoDto> Adultos);
