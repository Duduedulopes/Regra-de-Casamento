using RegraDeCasamento.Domain.Familias;

namespace RegraDeCasamento.Domain.Financas;

public sealed record ContribuicaoDoAdulto(Guid MembroId, string Nome, decimal Valor, decimal Porcentagem);

/// <summary>
/// Quanto cada adulto pagou das contas da casa no mês (R9). É só registro histórico:
/// não existe dívida entre o casal, então nada aqui é cobrança.
/// </summary>
public sealed record ContribuicaoDoMes(int Ano, int Mes, decimal TotalDaCasa, IReadOnlyList<ContribuicaoDoAdulto> Adultos)
{
    /// <summary>
    /// Entram só as despesas da casa (sem dono) que foram pagas no mês. Despesa de um adulto não entra (R6, R9).
    /// </summary>
    public static ContribuicaoDoMes Calcular(Familia familia, IEnumerable<Despesa> despesas, int ano, int mes)
    {
        var pagasNoMes = despesas
            .Where(d => d.FamiliaId == familia.Id
                && d.EhDaCasa
                && d.Situacao == SituacaoDaDespesa.Paga
                && d.PagaEm is { } dia && dia.Year == ano && dia.Month == mes)
            .ToList();

        var total = pagasNoMes.Sum(d => d.Valor);

        var adultos = familia.Membros
            .Where(m => m.Perfil == PerfilDoMembro.Adulto)
            .OrderBy(m => m.CriadoEmUtc)
            .Select(adulto =>
            {
                var valor = pagasNoMes.Where(d => d.PagaPorId == adulto.Id).Sum(d => d.Valor);
                var porcentagem = total == 0 ? 0 : decimal.Round(valor / total * 100, 1, MidpointRounding.AwayFromZero);
                return new ContribuicaoDoAdulto(adulto.Id, adulto.Nome, valor, porcentagem);
            })
            .ToList();

        return new ContribuicaoDoMes(ano, mes, total, adultos);
    }
}
