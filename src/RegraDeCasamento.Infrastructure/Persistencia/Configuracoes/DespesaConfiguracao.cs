using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RegraDeCasamento.Domain.Familias;
using RegraDeCasamento.Domain.Financas;

namespace RegraDeCasamento.Infrastructure.Persistencia.Configuracoes;

internal sealed class DespesaConfiguracao : IEntityTypeConfiguration<Despesa>
{
    public void Configure(EntityTypeBuilder<Despesa> builder)
    {
        builder.ToTable("Despesas");

        builder.Property(despesa => despesa.Descricao).HasMaxLength(Despesa.TamanhoMaximoDaDescricao);
        builder.Property(despesa => despesa.Valor).HasPrecision(12, 2);
        builder.Property(despesa => despesa.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Ignore(despesa => despesa.EhDaCasa);

        builder.HasOne<Familia>().WithMany().HasForeignKey(despesa => despesa.FamiliaId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(despesa => new { despesa.FamiliaId, despesa.Vencimento });
        builder.HasIndex(despesa => new { despesa.FamiliaId, despesa.PagaEm });

        // A parcela aponta para a dívida dela (R38). Apagar a dívida não apaga parcelas já registradas.
        builder.HasOne<Divida>().WithMany().HasForeignKey(despesa => despesa.DividaId).OnDelete(DeleteBehavior.Restrict);
    }
}
