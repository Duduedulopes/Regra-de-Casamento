using RegraDeCasamento.Shared.Financas;
using RegraDeCasamento.Shared.Mercado;

namespace RegraDeCasamento.Application.Relatorios;

/// <summary>Gera as planilhas .xlsx dos relatórios. Quem implementa é a Infrastructure (ClosedXML).</summary>
public interface IGeradorDePlanilhas
{
    /// <summary>A contribuição do mês (R9) e, numa segunda aba, as contas da casa pagas que entraram na conta.</summary>
    byte[] Contribuicao(ContribuicaoDoMesDto contribuicao, IReadOnlyList<DespesaDto> contasDaCasaPagas);

    /// <summary>A planilha de controle do mercado (R13): uma linha por item comprado e uma aba de resumo.</summary>
    byte[] Mercado(ResumoDoMercadoDto resumo, IReadOnlyList<CompraDto> compras);
}
