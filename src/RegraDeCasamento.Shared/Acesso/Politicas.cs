namespace RegraDeCasamento.Shared.Acesso;

/// <summary>As policies de autorização. Quem confere é sempre o servidor; a tela só usa para esconder botões.</summary>
public static class Politicas
{
    /// <summary>Só adultos (módulos marcados "só adultos" na ARCHITECTURE.md).</summary>
    public const string Adulto = "Adulto";
}
