using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RegraDeCasamento.Domain.Familias;

namespace RegraDeCasamento.Infrastructure.Persistencia.Configuracoes;

internal sealed class FamiliaConfiguracao : IEntityTypeConfiguration<Familia>
{
    public void Configure(EntityTypeBuilder<Familia> builder)
    {
        builder.ToTable("Familias");

        builder.Property(familia => familia.Nome).HasMaxLength(Familia.TamanhoMaximoDoNome);
        builder.Property(familia => familia.Codigo).HasMaxLength(Familia.TamanhoDoCodigo).IsFixedLength();
        builder.HasIndex(familia => familia.Codigo).IsUnique();

        builder.HasMany(familia => familia.Membros)
            .WithOne()
            .HasForeignKey(membro => membro.FamiliaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(familia => familia.PedidosDeEntrada)
            .WithOne()
            .HasForeignKey(pedido => pedido.FamiliaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
