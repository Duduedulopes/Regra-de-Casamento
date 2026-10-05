using RegraDeCasamento.Domain.Familias;

namespace RegraDeCasamento.Domain.Mercado;

public sealed record TotalPorFinalidade(FinalidadeDaCompra Finalidade, decimal Total, int Compras);

public sealed record TotalPorComprador(Guid MembroId, string Nome, decimal Total, int Compras);

/// <summary>O controle do mercado no mês (R13): quanto foi gasto, para quê e por quem.</summary>
public sealed record ResumoDoMercado(
    int Ano,
    int Mes,
    decimal Total,
    IReadOnlyList<TotalPorFinalidade> PorFinalidade,
    IReadOnlyList<TotalPorComprador> PorComprador)
{
    public static ResumoDoMercado Calcular(Familia familia, IEnumerable<Compra> compras, int ano, int mes)
    {
        var doMes = compras
            .Where(c => c.FamiliaId == familia.Id && c.Dia.Year == ano && c.Dia.Month == mes)
            .ToList();

        var porFinalidade = Enum.GetValues<FinalidadeDaCompra>()
            .Select(finalidade =>
            {
                var compras = doMes.Where(c => c.Finalidade == finalidade).ToList();
                return new TotalPorFinalidade(finalidade, compras.Sum(c => c.Total), compras.Count);
            })
            .ToList();

        var porComprador = familia.Membros
            .Where(m => m.Perfil == PerfilDoMembro.Adulto)
            .OrderBy(m => m.CriadoEmUtc)
            .Select(adulto =>
            {
                var compras = doMes.Where(c => c.CompradoPorId == adulto.Id).ToList();
                return new TotalPorComprador(adulto.Id, adulto.Nome, compras.Sum(c => c.Total), compras.Count);
            })
            .ToList();

        return new ResumoDoMercado(ano, mes, doMes.Sum(c => c.Total), porFinalidade, porComprador);
    }
}
