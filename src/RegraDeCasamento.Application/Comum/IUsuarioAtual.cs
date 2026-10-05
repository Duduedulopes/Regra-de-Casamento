namespace RegraDeCasamento.Application.Comum;

/// <summary>Quem está usando o sistema agora, tirado do login.</summary>
public interface IUsuarioAtual
{
    /// <summary>A conta de login. Null sem login.</summary>
    Guid? ContaId { get; }

    /// <summary>O membro da família desta conta. Null enquanto a conta não entrou numa família.</summary>
    Guid? MembroId { get; }
}
