using RegraDeCasamento.Application.Familias;
using RegraDeCasamento.Domain.Familias;

namespace RegraDeCasamento.Application.Tests.Familias;

public class ListarPedidosDeEntradaPendentesTests
{
    [Fact]
    public async Task R34_ListaSoOsPedidosQueEsperamResposta()
    {
        var familia = Familia.Criar("Família Silva", "Ana");
        var ana = familia.Membros.Single();
        var recusado = familia.ReceberPedidoDeEntrada(Guid.CreateVersion7(), "Carla").Valor;
        var esperando = familia.ReceberPedidoDeEntrada(Guid.CreateVersion7(), "Bruno").Valor;
        familia.RecusarPedidoDeEntrada(recusado.Id, ana.Id);

        var usuario = new UsuarioFixo { MembroId = ana.Id };
        var familias = new FamiliasEmMemoria(usuario);
        familias.Adicionar(familia);

        var pedidos = await new ListarPedidosDeEntradaPendentes(familias).ExecutarAsync();

        var pedido = Assert.Single(pedidos);
        Assert.Equal(esperando.Id, pedido.Id);
        Assert.Equal("Bruno", pedido.NomeDoSolicitante);
    }

    [Fact]
    public async Task SemFamilia_NaoHaPedidos()
    {
        var familias = new FamiliasEmMemoria(new UsuarioFixo());

        var pedidos = await new ListarPedidosDeEntradaPendentes(familias).ExecutarAsync();

        Assert.Empty(pedidos);
    }
}
