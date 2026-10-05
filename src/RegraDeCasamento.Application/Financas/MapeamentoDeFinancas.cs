using RegraDeCasamento.Domain.Comum;
using RegraDeCasamento.Domain.Familias;
using RegraDeCasamento.Domain.Financas;
using RegraDeCasamento.Shared.Financas;

namespace RegraDeCasamento.Application.Financas;

public static class MapeamentoDeFinancas
{
    public const string NomeDaCasa = "Casa";

    public static DespesaDto ParaDto(Despesa despesa, Familia familia) => new(
        despesa.Id,
        despesa.Descricao,
        despesa.Valor,
        despesa.Vencimento,
        despesa.DonoId,
        NomeDoDono(familia, despesa.DonoId),
        despesa.Situacao == SituacaoDaDespesa.Paga ? SituacaoDeConta.Paga : SituacaoDeConta.APagar,
        despesa.PagaPorId,
        despesa.PagaPorId is { } pagaPorId ? NomeDoMembro(familia, pagaPorId) : null,
        despesa.PagaEm,
        despesa.DividaId);

    public static DividaDto ParaDto(Divida divida, IEnumerable<Despesa> parcelas, Familia familia)
    {
        var andamento = AndamentoDaDivida.Calcular(divida, parcelas);
        return new DividaDto(
            divida.Id,
            divida.Descricao,
            (TipoDeDividaDto)divida.Tipo,
            divida.ValorTotal,
            divida.NumeroDeParcelas,
            divida.PrimeiroVencimento,
            divida.DonoId,
            NomeDoDono(familia, divida.DonoId),
            andamento.ParcelasPagas,
            andamento.ValorPago,
            andamento.ValorQueFalta);
    }

    public static NovaDivida ParaDominio(DadosDaNovaDivida dados) => new(
        dados.Descricao,
        (TipoDeDivida)dados.Tipo,
        dados.ValorTotal,
        dados.NumeroDeParcelas,
        dados.PrimeiroVencimento,
        dados.DonoId);

    public static RendaDto ParaDto(Renda renda, Familia familia) => new(
        renda.Id,
        renda.Descricao,
        renda.Valor,
        renda.DonoId,
        NomeDoDono(familia, renda.DonoId),
        (TipoDeRendaDto)renda.Tipo,
        renda.DiaDeRecebimento,
        renda.EmDiaUtil,
        renda.DataDeFim,
        renda.UsoRestrito);

    public static NovaRenda ParaDominio(DadosDaNovaRenda dados) => new(
        dados.Descricao,
        dados.Valor,
        dados.DonoId,
        Enum.IsDefined((TipoDeRenda)dados.Tipo) ? (TipoDeRenda)dados.Tipo : throw new DadosInvalidosException("Escolha um tipo de renda válido."),
        dados.DiaDeRecebimento,
        dados.EmDiaUtil,
        dados.DataDeFim,
        dados.UsoRestrito);

    public static ContribuicaoDoMesDto ParaDto(ContribuicaoDoMes contribuicao) => new(
        contribuicao.Ano,
        contribuicao.Mes,
        contribuicao.TotalDaCasa,
        contribuicao.Adultos.Select(a => new ContribuicaoDoAdultoDto(a.MembroId, a.Nome, a.Valor, a.Porcentagem)).ToList());

    private static string NomeDoDono(Familia familia, Guid? donoId) =>
        donoId is { } id ? NomeDoMembro(familia, id) : NomeDaCasa;

    private static string NomeDoMembro(Familia familia, Guid membroId) =>
        familia.Membros.FirstOrDefault(m => m.Id == membroId)?.Nome ?? "?";
}
