using RegraDeCasamento.Domain.Comum;

namespace RegraDeCasamento.Domain.Tests.Comum;

public class IPertenceAFamiliaTests
{
    [Fact]
    public void TudoQuePertenceAUmaFamilia_EEntidade()
    {
        // O filtro do banco (R5) é aplicado às entidades; algo da família que não fosse entidade escaparia dele.
        var tiposDaFamilia = typeof(Entidade).Assembly.GetTypes()
            .Where(tipo => tipo is { IsClass: true, IsAbstract: false } && typeof(IPertenceAFamilia).IsAssignableFrom(tipo))
            .ToList();

        Assert.All(tiposDaFamilia, tipo => Assert.True(tipo.IsSubclassOf(typeof(Entidade)), $"{tipo.Name} pertence a uma família mas não é Entidade."));
    }
}
