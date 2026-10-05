using RegraDeCasamento.Domain.Familias;
using RegraDeCasamento.Shared.Acesso;
using RegraDeCasamento.Shared.Familias;

namespace RegraDeCasamento.Application.Familias;

public static class MapeamentoDeFamilia
{
    public static FamiliaDto ParaDto(Familia familia) => new(
        familia.Id,
        familia.Nome,
        familia.Codigo,
        familia.Membros
            .OrderBy(membro => membro.Perfil)
            .ThenBy(membro => membro.CriadoEmUtc)
            .Select(membro => new MembroDto(membro.Id, membro.Nome, PerfilDeAcessoDo(membro), membro.Apelido))
            .ToList());

    public static PerfilDeAcesso PerfilDeAcessoDo(Membro membro) => membro.Perfil switch
    {
        PerfilDoMembro.Adulto => PerfilDeAcesso.Adulto,
        PerfilDoMembro.Crianca => PerfilDeAcesso.Crianca,
        _ => throw new InvalidOperationException($"Perfil de membro desconhecido: {membro.Perfil}."),
    };
}
