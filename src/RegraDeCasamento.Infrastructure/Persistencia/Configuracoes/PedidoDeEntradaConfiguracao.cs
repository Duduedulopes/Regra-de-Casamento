using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RegraDeCasamento.Domain.Familias;

namespace RegraDeCasamento.Infrastructure.Persistencia.Configuracoes;

internal sealed class PedidoDeEntradaConfiguracao : IEntityTypeConfiguration<PedidoDeEntrada>
{
    public void Configure(EntityTypeBuilder<PedidoDeEntrada> builder)
    {
        builder.ToTable("PedidosDeEntrada");

        builder.Property(pedido => pedido.NomeDoSolicitante).HasMaxLength(Membro.TamanhoMaximoDoNome);
        builder.Property(pedido => pedido.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(pedido => pedido.ContaDoSolicitanteId);
    }
}
