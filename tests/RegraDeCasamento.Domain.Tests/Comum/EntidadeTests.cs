using RegraDeCasamento.Domain.Comum;

namespace RegraDeCasamento.Domain.Tests.Comum;

public class EntidadeTests
{
    private sealed class EntidadeDeTeste : Entidade
    {
        public void Atualizar() => RegistrarAtualizacao();
    }

    [Fact]
    public void Id_NasceNoCSharp_NuncaVazio()
    {
        var entidade = new EntidadeDeTeste();

        Assert.NotEqual(Guid.Empty, entidade.Id);
    }

    [Fact]
    public void Id_EDiferenteACadaEntidade()
    {
        var ids = Enumerable.Range(0, 1000).Select(_ => new EntidadeDeTeste().Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void Id_EGuidVersao7_ParaFicarEmOrdemNoBanco()
    {
        var entidade = new EntidadeDeTeste();

        Assert.Equal(7, entidade.Id.Version);
    }

    [Fact]
    public void CriadoEmUtc_EstaEmUtc_ENoMomentoDaCriacao()
    {
        var antes = DateTime.UtcNow;
        var entidade = new EntidadeDeTeste();
        var depois = DateTime.UtcNow;

        Assert.Equal(DateTimeKind.Utc, entidade.CriadoEmUtc.Kind);
        Assert.InRange(entidade.CriadoEmUtc, antes, depois);
    }

    [Fact]
    public void AtualizadoEmUtc_ComecaVazio()
    {
        var entidade = new EntidadeDeTeste();

        Assert.Null(entidade.AtualizadoEmUtc);
    }

    [Fact]
    public void RegistrarAtualizacao_GuardaODataEmUtc()
    {
        var entidade = new EntidadeDeTeste();
        var antes = DateTime.UtcNow;

        entidade.Atualizar();

        var atualizadoEm = Assert.NotNull(entidade.AtualizadoEmUtc);
        Assert.Equal(DateTimeKind.Utc, atualizadoEm.Kind);
        Assert.True(atualizadoEm >= antes);
    }
}
