using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RegraDeCasamento.Domain.Familias;
using RegraDeCasamento.Infrastructure.Acesso;

namespace RegraDeCasamento.Infrastructure.Persistencia.Configuracoes;

internal sealed class ContaConfiguracao : IEntityTypeConfiguration<Conta>
{
    public void Configure(EntityTypeBuilder<Conta> builder)
    {
        builder.Property(conta => conta.Id).ValueGeneratedNever();
        builder.Property(conta => conta.TipoDeLogin).HasConversion<string>().HasMaxLength(20);

        // R1: cada membro tem o próprio login, então um membro nunca fica com duas contas.
        builder.HasOne<Membro>()
            .WithOne()
            .HasForeignKey<Conta>(conta => conta.MembroId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
