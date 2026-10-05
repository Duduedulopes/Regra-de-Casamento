using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RegraDeCasamento.Domain.Familias;

namespace RegraDeCasamento.Infrastructure.Persistencia.Configuracoes;

internal sealed class MembroConfiguracao : IEntityTypeConfiguration<Membro>
{
    public void Configure(EntityTypeBuilder<Membro> builder)
    {
        builder.ToTable("Membros");

        builder.Property(membro => membro.Nome).HasMaxLength(Membro.TamanhoMaximoDoNome);
        builder.Property(membro => membro.Apelido).HasMaxLength(Membro.TamanhoMaximoDoApelido);
        builder.Property(membro => membro.Perfil).HasConversion<string>().HasMaxLength(20);

        // R35: o apelido não se repete dentro da família. Adultos não têm apelido, e nulos não colidem.
        builder.HasIndex(membro => new { membro.FamiliaId, membro.Apelido }).IsUnique();
    }
}
