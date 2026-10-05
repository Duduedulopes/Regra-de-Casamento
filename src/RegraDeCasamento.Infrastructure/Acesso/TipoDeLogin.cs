namespace RegraDeCasamento.Infrastructure.Acesso;

/// <summary>Como a conta entra no app. Define as regras da senha (ver ValidadorDeSenha).</summary>
public enum TipoDeLogin
{
    /// <summary>Adulto: e-mail e senha forte.</summary>
    EmailESenha,

    /// <summary>Criança: "apelido@CODIGO" e PIN de 4 a 6 números (R35).</summary>
    ApelidoEPin,
}
