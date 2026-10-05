namespace RegraDeCasamento.Shared.Acesso;

/// <summary>Cadastro de um adulto. Crianças não se cadastram: um adulto cria a conta delas (R35).</summary>
public sealed record DadosDeCadastro(string Email, string Senha);
