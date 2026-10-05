using Microsoft.AspNetCore.Identity;

namespace RegraDeCasamento.Infrastructure.Acesso;

/// <summary>
/// Adulto usa senha forte (6 ou mais, com maiúscula, minúscula, número e símbolo).
/// Criança usa PIN de 4 a 6 números (R35), e as tentativas erradas bloqueiam a conta por um tempo.
/// </summary>
public sealed class ValidadorDeSenha : IPasswordValidator<Conta>
{
    public const int TamanhoMinimoDoPin = 4;
    public const int TamanhoMaximoDoPin = 6;
    public const int TamanhoMinimoDaSenha = 6;

    public Task<IdentityResult> ValidateAsync(UserManager<Conta> manager, Conta user, string? password)
    {
        var senha = password ?? "";
        var erros = user.TipoDeLogin == TipoDeLogin.ApelidoEPin
            ? ErrosDoPin(senha)
            : ErrosDaSenha(senha, manager.ErrorDescriber);

        return Task.FromResult(erros.Count == 0 ? IdentityResult.Success : IdentityResult.Failed([.. erros]));
    }

    private static List<IdentityError> ErrosDoPin(string pin) =>
        pin.Length is >= TamanhoMinimoDoPin and <= TamanhoMaximoDoPin && pin.All(char.IsAsciiDigit)
            ? []
            : [new IdentityError { Code = "PinInvalido", Description = $"O PIN precisa ter de {TamanhoMinimoDoPin} a {TamanhoMaximoDoPin} números." }];

    private static List<IdentityError> ErrosDaSenha(string senha, IdentityErrorDescriber descricoes)
    {
        var erros = new List<IdentityError>();
        if (senha.Length < TamanhoMinimoDaSenha)
        {
            erros.Add(descricoes.PasswordTooShort(TamanhoMinimoDaSenha));
        }

        if (!senha.Any(char.IsAsciiDigit))
        {
            erros.Add(descricoes.PasswordRequiresDigit());
        }

        if (!senha.Any(char.IsLower))
        {
            erros.Add(descricoes.PasswordRequiresLower());
        }

        if (!senha.Any(char.IsUpper))
        {
            erros.Add(descricoes.PasswordRequiresUpper());
        }

        if (senha.All(char.IsLetterOrDigit))
        {
            erros.Add(descricoes.PasswordRequiresNonAlphanumeric());
        }

        return erros;
    }
}
