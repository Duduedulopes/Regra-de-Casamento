using System.Text.Json.Serialization;

namespace RegraDeCasamento.Shared.Acesso;

/// <summary>Os dois perfis de quem usa o sistema (R2 a R4).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<PerfilDeAcesso>))]
public enum PerfilDeAcesso
{
    Adulto,
    Crianca,
}
