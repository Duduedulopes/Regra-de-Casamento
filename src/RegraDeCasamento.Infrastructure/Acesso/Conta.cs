using Microsoft.AspNetCore.Identity;

namespace RegraDeCasamento.Infrastructure.Acesso;

/// <summary>
/// A conta de login (ASP.NET Core Identity). Cada membro tem a sua (R1).
/// Uma conta sem membro ainda não entrou em nenhuma família.
/// </summary>
public sealed class Conta : IdentityUser<Guid>
{
    public Conta()
    {
        // O Id nasce no C#, como nas entidades do Domain (STACK.md, regra 2).
        Id = Guid.CreateVersion7();
    }

    public Guid? MembroId { get; set; }

    public TipoDeLogin TipoDeLogin { get; set; } = TipoDeLogin.EmailESenha;
}
