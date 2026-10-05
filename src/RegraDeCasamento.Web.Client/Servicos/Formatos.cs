using System.Globalization;

namespace RegraDeCasamento.Web.Client.Servicos;

/// <summary>Como as telas mostram datas e valores (horário e formato do Brasil).</summary>
public static class Formatos
{
    public static readonly CultureInfo PortuguesDoBrasil = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>"Outubro de 2026": só a primeira letra em maiúscula.</summary>
    public static string MesPorExtenso(DateOnly mes)
    {
        var texto = mes.ToString("MMMM 'de' yyyy", PortuguesDoBrasil);
        return char.ToUpper(texto[0], PortuguesDoBrasil) + texto[1..];
    }

    public static string Reais(decimal valor) => valor.ToString("C", PortuguesDoBrasil);
}
