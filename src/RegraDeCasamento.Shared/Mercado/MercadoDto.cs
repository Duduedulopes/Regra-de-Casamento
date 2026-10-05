using System.Text.Json.Serialization;

namespace RegraDeCasamento.Shared.Mercado;

/// <summary>Para que a compra foi feita (R12).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<FinalidadeDaCompraDto>))]
public enum FinalidadeDaCompraDto
{
    SuprimentoDaCasa,
    LancheParaTodos,
    Pessoal,
}

public sealed record ItemDaNovaCompra(string Descricao, decimal Quantidade, decimal ValorUnitario);

/// <summary>
/// Uma compra nova. Quem comprou é quem está lançando (R37). <see cref="ParaQuemId"/> só vale na compra pessoal (R12).
/// </summary>
public sealed record DadosDaNovaCompra(
    DateOnly Dia,
    string Local,
    FinalidadeDaCompraDto Finalidade,
    Guid? ParaQuemId,
    IReadOnlyList<ItemDaNovaCompra> Itens);

public sealed record ItemDaCompraDto(string Descricao, decimal Quantidade, decimal ValorUnitario, decimal Total);

public sealed record CompraDto(
    Guid Id,
    DateOnly Dia,
    string Local,
    Guid CompradoPorId,
    string CompradoPor,
    FinalidadeDaCompraDto Finalidade,
    Guid? ParaQuemId,
    string? ParaQuem,
    decimal Total,
    IReadOnlyList<ItemDaCompraDto> Itens);

public sealed record TotalPorFinalidadeDto(FinalidadeDaCompraDto Finalidade, decimal Total, int Compras);

public sealed record TotalPorCompradorDto(Guid MembroId, string Nome, decimal Total, int Compras);

/// <summary>O controle do mercado no mês (R13).</summary>
public sealed record ResumoDoMercadoDto(
    int Ano,
    int Mes,
    decimal Total,
    IReadOnlyList<TotalPorFinalidadeDto> PorFinalidade,
    IReadOnlyList<TotalPorCompradorDto> PorComprador);
