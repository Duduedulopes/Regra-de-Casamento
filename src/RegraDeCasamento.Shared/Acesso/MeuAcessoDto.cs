namespace RegraDeCasamento.Shared.Acesso;

/// <summary>Quem está logado. Membro, família e perfil ficam vazios enquanto a conta não entrou numa família.</summary>
public sealed record MeuAcessoDto(string Usuario, Guid? MembroId, Guid? FamiliaId, PerfilDeAcesso? Perfil);
