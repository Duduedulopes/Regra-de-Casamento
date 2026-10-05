using System.Globalization;
using ClosedXML.Excel;
using RegraDeCasamento.Application.Relatorios;
using RegraDeCasamento.Shared.Financas;
using RegraDeCasamento.Shared.Mercado;

namespace RegraDeCasamento.Infrastructure.Relatorios;

public sealed class GeradorDePlanilhas : IGeradorDePlanilhas
{
    private const string FormatoDeReais = "\"R$\" #,##0.00";
    private const string FormatoDeData = "dd/mm/yyyy";
    private static readonly CultureInfo PortuguesDoBrasil = CultureInfo.GetCultureInfo("pt-BR");

    public byte[] Contribuicao(ContribuicaoDoMesDto contribuicao, IReadOnlyList<DespesaDto> contasDaCasaPagas)
    {
        using var pasta = new XLWorkbook();
        var mes = new DateOnly(contribuicao.Ano, contribuicao.Mes, 1).ToString("MMMM 'de' yyyy", PortuguesDoBrasil);

        var resumo = pasta.Worksheets.Add("Contribuição");
        resumo.Cell(1, 1).Value = $"Contribuição nas contas da casa — {mes}";
        resumo.Cell(1, 1).Style.Font.Bold = true;
        resumo.Cell(2, 1).Value = "Só registro histórico: não existe dívida entre o casal (R9).";
        resumo.Cell(2, 1).Style.Font.Italic = true;

        Cabecalho(resumo, 4, "Adulto", "Pagou", "% das contas da casa");
        var linha = 5;
        foreach (var adulto in contribuicao.Adultos)
        {
            resumo.Cell(linha, 1).Value = adulto.Nome;
            resumo.Cell(linha, 2).Value = adulto.Valor;
            resumo.Cell(linha, 3).Value = adulto.Porcentagem / 100;
            linha++;
        }

        resumo.Cell(linha, 1).Value = "Total da casa";
        resumo.Cell(linha, 2).Value = contribuicao.TotalDaCasa;
        resumo.Row(linha).Style.Font.Bold = true;
        resumo.Range(5, 2, linha, 2).Style.NumberFormat.Format = FormatoDeReais;
        resumo.Range(5, 3, linha, 3).Style.NumberFormat.Format = "0.0%";
        resumo.Columns().AdjustToContents();

        var contas = pasta.Worksheets.Add("Contas da casa pagas");
        Cabecalho(contas, 1, "Conta", "Vencimento", "Valor", "Pago por", "Pago em");
        linha = 2;
        foreach (var conta in contasDaCasaPagas)
        {
            contas.Cell(linha, 1).Value = conta.Descricao;
            contas.Cell(linha, 2).Value = conta.Vencimento.ToDateTime(TimeOnly.MinValue);
            contas.Cell(linha, 3).Value = conta.Valor;
            contas.Cell(linha, 4).Value = conta.PagaPor ?? "";
            contas.Cell(linha, 5).Value = conta.PagaEm?.ToDateTime(TimeOnly.MinValue);
            linha++;
        }

        contas.Column(2).Style.NumberFormat.Format = FormatoDeData;
        contas.Column(3).Style.NumberFormat.Format = FormatoDeReais;
        contas.Column(5).Style.NumberFormat.Format = FormatoDeData;
        contas.Columns().AdjustToContents();

        using var memoria = new MemoryStream();
        pasta.SaveAs(memoria);
        return memoria.ToArray();
    }

    public byte[] Mercado(ResumoDoMercadoDto resumo, IReadOnlyList<CompraDto> compras)
    {
        using var pasta = new XLWorkbook();
        var mes = new DateOnly(resumo.Ano, resumo.Mes, 1).ToString("MMMM 'de' yyyy", PortuguesDoBrasil);

        var itens = pasta.Worksheets.Add("Compras");
        Cabecalho(itens, 1, "Dia", "Local", "Comprado por", "Para quê", "Para quem", "Item", "Quantidade", "Valor unitário", "Total do item");
        var linha = 2;
        foreach (var compra in compras)
        {
            foreach (var item in compra.Itens)
            {
                itens.Cell(linha, 1).Value = compra.Dia.ToDateTime(TimeOnly.MinValue);
                itens.Cell(linha, 2).Value = compra.Local;
                itens.Cell(linha, 3).Value = compra.CompradoPor;
                itens.Cell(linha, 4).Value = NomeDaFinalidade(compra.Finalidade);
                itens.Cell(linha, 5).Value = compra.ParaQuem ?? "";
                itens.Cell(linha, 6).Value = item.Descricao;
                itens.Cell(linha, 7).Value = item.Quantidade;
                itens.Cell(linha, 8).Value = item.ValorUnitario;
                itens.Cell(linha, 9).Value = item.Total;
                linha++;
            }
        }

        itens.Column(1).Style.NumberFormat.Format = FormatoDeData;
        itens.Column(8).Style.NumberFormat.Format = FormatoDeReais;
        itens.Column(9).Style.NumberFormat.Format = FormatoDeReais;
        itens.Columns().AdjustToContents();

        var aba = pasta.Worksheets.Add("Resumo");
        aba.Cell(1, 1).Value = $"Controle do mercado — {mes}";
        aba.Cell(1, 1).Style.Font.Bold = true;

        Cabecalho(aba, 3, "Para quê", "Compras", "Total");
        linha = 4;
        foreach (var finalidade in resumo.PorFinalidade)
        {
            aba.Cell(linha, 1).Value = NomeDaFinalidade(finalidade.Finalidade);
            aba.Cell(linha, 2).Value = finalidade.Compras;
            aba.Cell(linha, 3).Value = finalidade.Total;
            linha++;
        }

        aba.Cell(linha, 1).Value = "Total do mês";
        aba.Cell(linha, 3).Value = resumo.Total;
        aba.Row(linha).Style.Font.Bold = true;
        aba.Range(4, 3, linha, 3).Style.NumberFormat.Format = FormatoDeReais;

        linha += 2;
        Cabecalho(aba, linha, "Quem comprou", "Compras", "Total");
        var primeiraDosCompradores = linha + 1;
        foreach (var comprador in resumo.PorComprador)
        {
            linha++;
            aba.Cell(linha, 1).Value = comprador.Nome;
            aba.Cell(linha, 2).Value = comprador.Compras;
            aba.Cell(linha, 3).Value = comprador.Total;
        }

        aba.Range(primeiraDosCompradores, 3, linha, 3).Style.NumberFormat.Format = FormatoDeReais;
        aba.Columns().AdjustToContents();

        using var memoria = new MemoryStream();
        pasta.SaveAs(memoria);
        return memoria.ToArray();
    }

    public static string NomeDaFinalidade(FinalidadeDaCompraDto finalidade) => finalidade switch
    {
        FinalidadeDaCompraDto.SuprimentoDaCasa => "Suprimento da casa",
        FinalidadeDaCompraDto.LancheParaTodos => "Lanche para todos",
        FinalidadeDaCompraDto.Pessoal => "Pessoal",
        _ => finalidade.ToString(),
    };

    private static void Cabecalho(IXLWorksheet aba, int linha, params string[] titulos)
    {
        for (var coluna = 0; coluna < titulos.Length; coluna++)
        {
            aba.Cell(linha, coluna + 1).Value = titulos[coluna];
        }

        var faixa = aba.Range(linha, 1, linha, titulos.Length);
        faixa.Style.Font.Bold = true;
        faixa.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8EAF6");
    }
}
