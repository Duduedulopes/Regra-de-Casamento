using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RegraDeCasamento.Domain.Familias;
using RegraDeCasamento.Domain.Mercado;

namespace RegraDeCasamento.Infrastructure.Persistencia.Configuracoes;

internal sealed class CompraConfiguracao : IEntityTypeConfiguration<Compra>
{
    public void Configure(EntityTypeBuilder<Compra> builder)
    {
        builder.ToTable("Compras");

        builder.Property(compra => compra.Local).HasMaxLength(Compra.TamanhoMaximoDoLocal);
        builder.Property(compra => compra.Finalidade).HasConversion<string>().HasMaxLength(30);
        builder.Ignore(compra => compra.Total);

        builder.HasOne<Familia>().WithMany().HasForeignKey(compra => compra.FamiliaId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(compra => new { compra.FamiliaId, compra.Dia });

        // Os itens só existem dentro da compra: tabela própria, mas carregados e gravados junto com ela.
        builder.OwnsMany(compra => compra.Itens, item =>
        {
            item.ToTable("ItensDaCompra");
            item.WithOwner().HasForeignKey("CompraId");
            item.HasKey(i => i.Id);
            item.Property(i => i.Id).ValueGeneratedNever();
            item.Property(i => i.Descricao).HasMaxLength(ItemDaCompra.TamanhoMaximoDaDescricao);
            item.Property(i => i.Quantidade).HasPrecision(12, 3);
            item.Property(i => i.ValorUnitario).HasPrecision(12, 2);
            item.Ignore(i => i.Total);
        });
        builder.Navigation(compra => compra.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
