namespace RegraDeCasamento.Application.Comum;

/// <summary>
/// A família de quem está usando o sistema agora, tirada do login. Sem login (ou sem família), é null.
/// O banco filtra todas as consultas por esse valor (R5).
/// </summary>
public interface IFamiliaAtual
{
    Guid? FamiliaId { get; }
}
