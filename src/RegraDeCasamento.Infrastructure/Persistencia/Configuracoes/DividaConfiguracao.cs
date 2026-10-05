using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RegraDeCasamento.Domain.Familias;
using RegraDeCasamento.Domain.Financas;

namespace RegraDeCasamento.Infrastructure.Persistencia.Configuracoes;

internal sealed class DividaConfiguracao : IEntityTypeConfiguration<Divida>
{
    public void Configure(EntityTypeBuilder<Divida> builder)
    {
        builder.ToTable("Dividas");

        builder.Property(divida => divida.Descricao).HasMaxLength(Divida.TamanhoMaximoDaDescricao);
        builder.Property(divida => divida.Tipo).HasConversion<string>().HasMaxLength(30);
        builder.Property(divida => divida.ValorTotal).HasPrecision(12, 2);

        builder.HasOne<Familia>().WithMany().HasForeignKey(divida => divida.FamiliaId).OnDelete(DeleteBehavior.Cascade);
    }
}
