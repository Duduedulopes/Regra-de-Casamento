using RegraDeCasamento.Domain.Comum;

namespace RegraDeCasamento.Domain.Familias;

/// <summary>
/// Uma pessoa da família: um adulto do casal ou uma criança (R1).
/// Só a <see cref="Familia"/> cria membros, para que as regras de entrada sempre sejam conferidas.
/// </summary>
public sealed class Membro : Entidade, IPertenceAFamilia
{
    public const int TamanhoMaximoDoNome = 100;
    public const int TamanhoMaximoDoApelido = 30;

    private Membro()
    {
    }

    public Guid FamiliaId { get; private set; }

    public string Nome { get; private set; } = "";

    public PerfilDoMembro Perfil { get; private set; }

    /// <summary>Como a criança é chamada na família, único dentro dela (R35). Adultos não têm.</summary>
    public string? Apelido { get; private set; }

    internal static Membro NovoAdulto(Guid familiaId, string nome) => new()
    {
        FamiliaId = familiaId,
        Nome = nome,
        Perfil = PerfilDoMembro.Adulto,
    };

    internal static Membro NovaCrianca(Guid familiaId, string nome, string apelido) => new()
    {
        FamiliaId = familiaId,
        Nome = nome,
        Perfil = PerfilDoMembro.Crianca,
        Apelido = apelido,
    };
}
