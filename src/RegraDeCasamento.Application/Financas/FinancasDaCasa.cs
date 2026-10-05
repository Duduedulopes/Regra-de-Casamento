using RegraDeCasamento.Application.Comum;
using RegraDeCasamento.Application.Familias;
using RegraDeCasamento.Application.Relatorios;
using RegraDeCasamento.Domain.Comum;
using RegraDeCasamento.Domain.Familias;
using RegraDeCasamento.Domain.Financas;
using RegraDeCasamento.Shared.Financas;

namespace RegraDeCasamento.Application.Financas;

/// <summary>
/// Os casos de uso das finanças: contas, rendas e a contribuição do mês. Só adultos chegam aqui
/// (policy Adulto no servidor), e o Domain confere de novo (R4).
/// </summary>
public sealed class FinancasDaCasa(
    IUsuarioAtual usuario,
    IRepositorioDeFamilias familias,
    IRepositorioDeFinancas financas,
    IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task<IReadOnlyList<DespesaDto>> ListarDespesasAsync(int ano, int mes, CancellationToken cancellationToken = default)
    {
        var (familia, _) = await FamiliaDeQuemUsaAsync(cancellationToken);
        var despesas = await financas.DespesasDoMesAsync(ano, mes, cancellationToken);
        return despesas
            .OrderBy(d => d.Vencimento)
            .ThenBy(d => d.Descricao)
            .Select(d => MapeamentoDeFinancas.ParaDto(d, familia))
            .ToList();
    }

    public async Task<ResultadoDeDominio<DespesaDto>> LancarDespesaAsync(DadosDaNovaDespesa dados, CancellationToken cancellationToken = default)
    {
        var (familia, adultoId) = await FamiliaDeQuemUsaAsync(cancellationToken);

        var despesa = Despesa.Lancar(familia, adultoId, dados.Descricao, dados.Valor, dados.Vencimento, dados.DonoId);
        if (despesa.Falhou)
        {
            return despesa.RepassarFalha<DespesaDto>();
        }

        financas.Adicionar(despesa.Valor);
        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        return ResultadoDeDominio.Ok(MapeamentoDeFinancas.ParaDto(despesa.Valor, familia));
    }

    /// <summary>Null quando a despesa não existe (ou é de outra família, que o filtro esconde).</summary>
    public Task<ResultadoDeDominio<DespesaDto>?> MarcarComoPagaAsync(Guid despesaId, DadosDoPagamento dados, CancellationToken cancellationToken = default) =>
        AlterarDespesaAsync(despesaId, (despesa, familia, adultoId) => despesa.MarcarComoPaga(familia, adultoId, dados.PagaPorId, dados.PagaEm), cancellationToken);

    public Task<ResultadoDeDominio<DespesaDto>?> MarcarComoAPagarAsync(Guid despesaId, CancellationToken cancellationToken = default) =>
        AlterarDespesaAsync(despesaId, (despesa, familia, adultoId) => despesa.MarcarComoAPagar(familia, adultoId), cancellationToken);

    public async Task<IReadOnlyList<RendaDto>> ListarRendasAsync(CancellationToken cancellationToken = default)
    {
        var (familia, _) = await FamiliaDeQuemUsaAsync(cancellationToken);
        var rendas = await financas.RendasAsync(cancellationToken);
        return rendas
            .OrderBy(r => r.DiaDeRecebimento)
            .ThenBy(r => r.Descricao)
            .Select(r => MapeamentoDeFinancas.ParaDto(r, familia))
            .ToList();
    }

    public async Task<ResultadoDeDominio<RendaDto>> LancarRendaAsync(DadosDaNovaRenda dados, CancellationToken cancellationToken = default)
    {
        var (familia, adultoId) = await FamiliaDeQuemUsaAsync(cancellationToken);

        var renda = Renda.Lancar(familia, adultoId, MapeamentoDeFinancas.ParaDominio(dados));
        if (renda.Falhou)
        {
            return renda.RepassarFalha<RendaDto>();
        }

        financas.Adicionar(renda.Valor);
        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        return ResultadoDeDominio.Ok(MapeamentoDeFinancas.ParaDto(renda.Valor, familia));
    }

    /// <summary>As dívidas da família com o andamento de cada uma (R38). Os dois adultos veem todas (R2).</summary>
    public async Task<IReadOnlyList<DividaDto>> ListarDividasAsync(CancellationToken cancellationToken = default)
    {
        var (familia, _) = await FamiliaDeQuemUsaAsync(cancellationToken);
        var dividas = await financas.DividasAsync(cancellationToken);
        var parcelas = await financas.ParcelasDasDividasAsync(cancellationToken);
        return dividas
            .OrderBy(d => d.PrimeiroVencimento)
            .Select(d => MapeamentoDeFinancas.ParaDto(d, parcelas, familia))
            .ToList();
    }

    /// <summary>Lança a dívida e as parcelas dela como contas a pagar, tudo de uma vez (R38).</summary>
    public async Task<ResultadoDeDominio<DividaDto>> LancarDividaAsync(DadosDaNovaDivida dados, CancellationToken cancellationToken = default)
    {
        var (familia, adultoId) = await FamiliaDeQuemUsaAsync(cancellationToken);

        var lancada = Divida.Lancar(familia, adultoId, MapeamentoDeFinancas.ParaDominio(dados));
        if (lancada.Falhou)
        {
            return lancada.RepassarFalha<DividaDto>();
        }

        financas.Adicionar(lancada.Valor.Divida);
        foreach (var parcela in lancada.Valor.Parcelas)
        {
            financas.Adicionar(parcela);
        }

        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        return ResultadoDeDominio.Ok(MapeamentoDeFinancas.ParaDto(lancada.Valor.Divida, lancada.Valor.Parcelas, familia));
    }

    /// <summary>A contribuição de cada adulto no mês (R9). Só registro histórico, nunca cobrança.</summary>
    public async Task<ContribuicaoDoMesDto> ContribuicaoDoMesAsync(int ano, int mes, CancellationToken cancellationToken = default) =>
        (await DadosDaContribuicaoAsync(ano, mes, cancellationToken)).Contribuicao;

    /// <summary>A planilha da contribuição, feita com a mesma consulta da tela.</summary>
    public async Task<byte[]> PlanilhaDaContribuicaoAsync(int ano, int mes, IGeradorDePlanilhas planilhas, CancellationToken cancellationToken = default)
    {
        var (contribuicao, contasDaCasaPagas) = await DadosDaContribuicaoAsync(ano, mes, cancellationToken);
        return planilhas.Contribuicao(contribuicao, contasDaCasaPagas);
    }

    private async Task<(ContribuicaoDoMesDto Contribuicao, IReadOnlyList<DespesaDto> ContasDaCasaPagas)> DadosDaContribuicaoAsync(
        int ano, int mes, CancellationToken cancellationToken)
    {
        var (familia, _) = await FamiliaDeQuemUsaAsync(cancellationToken);
        var despesas = await financas.DespesasDoMesAsync(ano, mes, cancellationToken);

        var contribuicao = ContribuicaoDoMes.Calcular(familia, despesas, ano, mes);
        var contasDaCasaPagas = despesas
            .Where(d => d.EhDaCasa && d.Situacao == SituacaoDaDespesa.Paga && d.PagaEm is { } dia && dia.Year == ano && dia.Month == mes)
            .OrderBy(d => d.PagaEm)
            .Select(d => MapeamentoDeFinancas.ParaDto(d, familia))
            .ToList();

        return (MapeamentoDeFinancas.ParaDto(contribuicao), contasDaCasaPagas);
    }

    private async Task<ResultadoDeDominio<DespesaDto>?> AlterarDespesaAsync(
        Guid despesaId,
        Func<Despesa, Familia, Guid, ResultadoDeDominio> alterar,
        CancellationToken cancellationToken)
    {
        var (familia, adultoId) = await FamiliaDeQuemUsaAsync(cancellationToken);
        var despesa = await financas.ObterDespesaAsync(despesaId, cancellationToken);
        if (despesa is null)
        {
            return null;
        }

        var resultado = alterar(despesa, familia, adultoId);
        if (resultado.Falhou)
        {
            return resultado.RepassarFalha<DespesaDto>();
        }

        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        return ResultadoDeDominio.Ok(MapeamentoDeFinancas.ParaDto(despesa, familia));
    }

    private async Task<(Familia Familia, Guid AdultoId)> FamiliaDeQuemUsaAsync(CancellationToken cancellationToken)
    {
        var adultoId = usuario.MembroId ?? throw new InvalidOperationException("Finanças exigem um membro logado.");
        var familia = await familias.ObterDaFamiliaAtualAsync(cancellationToken)
            ?? throw new InvalidOperationException("Finanças exigem uma família.");
        return (familia, adultoId);
    }
}
