using RegraDeCasamento.Application.Comum;
using RegraDeCasamento.Application.Familias;
using RegraDeCasamento.Application.Relatorios;
using RegraDeCasamento.Domain.Comum;
using RegraDeCasamento.Domain.Familias;
using RegraDeCasamento.Domain.Mercado;
using RegraDeCasamento.Shared.Mercado;

namespace RegraDeCasamento.Application.Mercado;

/// <summary>
/// Os casos de uso do mercado: lançar compra, ver as compras do mês, o resumo e a planilha de controle (R12, R13).
/// Só adultos chegam aqui (policy Adulto no servidor), e o Domain confere de novo (R4).
/// </summary>
public sealed class MercadoDaFamilia(
    IUsuarioAtual usuario,
    IRepositorioDeFamilias familias,
    IRepositorioDeCompras compras,
    IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task<ResultadoDeDominio<CompraDto>> LancarAsync(DadosDaNovaCompra dados, CancellationToken cancellationToken = default)
    {
        var (familia, adultoId) = await FamiliaDeQuemUsaAsync(cancellationToken);

        var compra = Compra.Lancar(familia, adultoId, ParaDominio(dados));
        if (compra.Falhou)
        {
            return compra.RepassarFalha<CompraDto>();
        }

        compras.Adicionar(compra.Valor);
        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        return ResultadoDeDominio.Ok(ParaDto(compra.Valor, familia));
    }

    public async Task<IReadOnlyList<CompraDto>> ComprasDoMesAsync(int ano, int mes, CancellationToken cancellationToken = default)
    {
        var (familia, doMes) = await DadosDoMesAsync(ano, mes, cancellationToken);
        return doMes.Select(c => ParaDto(c, familia)).ToList();
    }

    public async Task<ResumoDoMercadoDto> ResumoAsync(int ano, int mes, CancellationToken cancellationToken = default)
    {
        var (familia, doMes) = await DadosDoMesAsync(ano, mes, cancellationToken);
        return ParaDto(ResumoDoMercado.Calcular(familia, doMes, ano, mes));
    }

    /// <summary>A planilha de controle do casal (R13), feita com a mesma consulta da tela.</summary>
    public async Task<byte[]> PlanilhaAsync(int ano, int mes, IGeradorDePlanilhas planilhas, CancellationToken cancellationToken = default)
    {
        var (familia, doMes) = await DadosDoMesAsync(ano, mes, cancellationToken);
        var resumo = ParaDto(ResumoDoMercado.Calcular(familia, doMes, ano, mes));
        return planilhas.Mercado(resumo, doMes.Select(c => ParaDto(c, familia)).ToList());
    }

    private async Task<(Familia Familia, List<Compra> DoMes)> DadosDoMesAsync(int ano, int mes, CancellationToken cancellationToken)
    {
        var (familia, _) = await FamiliaDeQuemUsaAsync(cancellationToken);
        var doMes = await compras.ComprasDoMesAsync(ano, mes, cancellationToken);
        return (familia, doMes.OrderBy(c => c.Dia).ThenBy(c => c.CriadoEmUtc).ToList());
    }

    private async Task<(Familia Familia, Guid AdultoId)> FamiliaDeQuemUsaAsync(CancellationToken cancellationToken)
    {
        var adultoId = usuario.MembroId ?? throw new InvalidOperationException("O mercado exige um membro logado.");
        var familia = await familias.ObterDaFamiliaAtualAsync(cancellationToken)
            ?? throw new InvalidOperationException("O mercado exige uma família.");
        return (familia, adultoId);
    }

    private static NovaCompra ParaDominio(DadosDaNovaCompra dados) => new(
        dados.Dia,
        dados.Local,
        (FinalidadeDaCompra)dados.Finalidade,
        dados.ParaQuemId,
        (dados.Itens ?? []).Select(i => new NovoItem(i.Descricao, i.Quantidade, i.ValorUnitario)).ToList());

    private static CompraDto ParaDto(Compra compra, Familia familia) => new(
        compra.Id,
        compra.Dia,
        compra.Local,
        compra.CompradoPorId,
        NomeDe(familia, compra.CompradoPorId),
        (FinalidadeDaCompraDto)compra.Finalidade,
        compra.ParaQuemId,
        compra.ParaQuemId is { } paraQuem ? NomeDe(familia, paraQuem) : null,
        compra.Total,
        compra.Itens.Select(i => new ItemDaCompraDto(i.Descricao, i.Quantidade, i.ValorUnitario, i.Total)).ToList());

    private static ResumoDoMercadoDto ParaDto(ResumoDoMercado resumo) => new(
        resumo.Ano,
        resumo.Mes,
        resumo.Total,
        resumo.PorFinalidade.Select(f => new TotalPorFinalidadeDto((FinalidadeDaCompraDto)f.Finalidade, f.Total, f.Compras)).ToList(),
        resumo.PorComprador.Select(c => new TotalPorCompradorDto(c.MembroId, c.Nome, c.Total, c.Compras)).ToList());

    private static string NomeDe(Familia familia, Guid membroId) =>
        familia.Membros.FirstOrDefault(m => m.Id == membroId)?.Nome ?? "?";
}
