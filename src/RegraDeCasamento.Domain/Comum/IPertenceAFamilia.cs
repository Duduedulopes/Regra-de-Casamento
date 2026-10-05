namespace RegraDeCasamento.Domain.Comum;

/// <summary>
/// Marca as entidades que pertencem a uma família. O banco filtra tudo pelo FamiliaId,
/// para que uma família nunca enxergue os dados de outra (R5).
/// </summary>
public interface IPertenceAFamilia
{
    Guid FamiliaId { get; }
}
