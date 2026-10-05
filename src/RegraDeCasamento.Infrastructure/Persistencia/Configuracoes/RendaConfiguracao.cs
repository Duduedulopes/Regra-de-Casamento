using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RegraDeCasamento.Domain.Familias;
using RegraDeCasamento.Domain.Financas;

namespace RegraDeCasamento.Infrastructure.Persistencia.Configuracoes;

internal sealed class RendaConfiguracao : IEntityTypeConfiguration<Renda>
{
    public void Configure(EntityTypeBuilder<Renda> builder)
    {
        builder.ToTable("Rendas");

        builder.Property(renda => renda.Descricao).HasMaxLength(Renda.TamanhoMaximoDaDescricao);
        builder.Property(renda => renda.Valor).HasPrecision(12, 2);
        builder.Property(renda => renda.Tipo).HasConversion<string>().HasMaxLength(30);
        builder.Property(renda => renda.UsoRestrito).HasMaxLength(Renda.TamanhoMaximoDoUso);

        builder.HasOne<Familia>().WithMany().HasForeignKey(renda => renda.FamiliaId).OnDelete(DeleteBehavior.Cascade);
    }
}
