using RegraDeCasamento.Domain.Comum;
using RegraDeCasamento.Domain.Familias;

namespace RegraDeCasamento.Domain.Financas;

/// <summary>Conferências comuns a despesas e rendas: quem lança e de quem é o dinheiro (R4, R5, R6).</summary>
internal static class DonoDoDinheiro
{
    /// <summary>Só um adulto da própria família lança ou mexe em dinheiro. Null quando pode.</summary>
    public static ResultadoDeDominio? ConferirQuemMexe(Familia familia, Guid adultoId)
    {
        var membro = familia.Membros.FirstOrDefault(m => m.Id == adultoId);
        if (membro is null)
        {
            return ResultadoDeDominio.Falha("R5", "Quem não é desta família não mexe no dinheiro dela.");
        }

        return membro.Perfil == PerfilDoMembro.Adulto
            ? null
            : ResultadoDeDominio.Falha("R4", "Contas, rendas e gastos são só dos adultos.");
    }

    /// <summary>O dono é a casa (null) ou um dos adultos da família (R6). Null quando está certo.</summary>
    public static ResultadoDeDominio? ConferirDono(Familia familia, Guid? donoId) =>
        donoId is null || EhAdultoDaFamilia(familia, donoId.Value)
            ? null
            : ResultadoDeDominio.Falha("R6", "O dono precisa ser a casa ou um dos adultos da família.");

    /// <summary>
    /// O que é de um adulto, só ele lança ou altera; o outro vê, mas não mexe. O que é da casa, os dois (R37).
    /// Null quando pode.
    /// </summary>
    public static ResultadoDeDominio? ConferirQuemAltera(Guid adultoId, Guid? donoId) =>
        donoId is null || donoId == adultoId
            ? null
            : ResultadoDeDominio.Falha("R37", "Isto é do outro adulto: você pode ver, mas só o dono altera.");

    public static bool EhAdultoDaFamilia(Familia familia, Guid membroId) =>
        familia.Membros.Any(m => m.Id == membroId && m.Perfil == PerfilDoMembro.Adulto);
}
