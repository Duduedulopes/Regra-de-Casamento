using RegraDeCasamento.Shared.Familias;

namespace RegraDeCasamento.Application.Familias;

/// <summary>A família de quem está logado: nome, código e membros. Null se a conta ainda não tem família.</summary>
public sealed class VerMinhaFamilia(IRepositorioDeFamilias familias)
{
    public async Task<FamiliaDto?> ExecutarAsync(CancellationToken cancellationToken = default)
    {
        var familia = await familias.ObterDaFamiliaAtualAsync(cancellationToken);
        return familia is null ? null : MapeamentoDeFamilia.ParaDto(familia);
    }
}
