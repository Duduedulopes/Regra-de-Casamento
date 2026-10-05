namespace RegraDeCasamento.Shared.Acesso;

/// <summary>Login da criança: o código da família, o apelido dela e o PIN (R35).</summary>
public sealed record DadosDeLoginDaCrianca(string CodigoDaFamilia, string Apelido, string Pin)
{
    /// <summary>O usuário da conta da criança, montado do mesmo jeito em que foi criado.</summary>
    public static string UsuarioDaCrianca(string apelido, string codigoDaFamilia) =>
        $"{(apelido ?? "").Trim()}@{(codigoDaFamilia ?? "").Trim().ToUpperInvariant()}";
}
