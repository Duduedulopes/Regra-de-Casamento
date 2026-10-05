using Microsoft.AspNetCore.Identity;

namespace RegraDeCasamento.Web.Acesso;

/// <summary>As mensagens do Identity que chegam a quem usa o app (cadastro e senha), em português.</summary>
public sealed class ErrosDoIdentityEmPortugues : IdentityErrorDescriber
{
    public override IdentityError DuplicateUserName(string userName) =>
        Erro(nameof(DuplicateUserName), $"Já existe uma conta com o usuário \"{userName}\".");

    public override IdentityError DuplicateEmail(string email) =>
        Erro(nameof(DuplicateEmail), $"Já existe uma conta com o e-mail \"{email}\".");

    public override IdentityError InvalidUserName(string? userName) =>
        Erro(nameof(InvalidUserName), "Informe um usuário válido.");

    public override IdentityError InvalidEmail(string? email) =>
        Erro(nameof(InvalidEmail), "Informe um e-mail válido.");

    public override IdentityError PasswordTooShort(int length) =>
        Erro(nameof(PasswordTooShort), $"A senha precisa ter pelo menos {length} caracteres.");

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        Erro(nameof(PasswordRequiresNonAlphanumeric), "A senha precisa ter pelo menos um símbolo (ex.: ! @ #).");

    public override IdentityError PasswordRequiresDigit() =>
        Erro(nameof(PasswordRequiresDigit), "A senha precisa ter pelo menos um número.");

    public override IdentityError PasswordRequiresLower() =>
        Erro(nameof(PasswordRequiresLower), "A senha precisa ter pelo menos uma letra minúscula.");

    public override IdentityError PasswordRequiresUpper() =>
        Erro(nameof(PasswordRequiresUpper), "A senha precisa ter pelo menos uma letra maiúscula.");

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        Erro(nameof(PasswordRequiresUniqueChars), $"A senha precisa ter pelo menos {uniqueChars} caracteres diferentes.");

    private static IdentityError Erro(string codigo, string descricao) => new() { Code = codigo, Description = descricao };
}
