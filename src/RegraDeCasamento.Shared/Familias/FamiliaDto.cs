using RegraDeCasamento.Shared.Acesso;

namespace RegraDeCasamento.Shared.Familias;

/// <summary>A família como os adultos a veem: o código serve para o outro adulto pedir entrada (R34).</summary>
public sealed record FamiliaDto(Guid Id, string Nome, string Codigo, IReadOnlyList<MembroDto> Membros);

public sealed record MembroDto(Guid Id, string Nome, PerfilDeAcesso Perfil, string? Apelido);
