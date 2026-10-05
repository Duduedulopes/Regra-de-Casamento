using RegraDeCasamento.Domain.Familias;

namespace RegraDeCasamento.Domain.Tests.Familias;

public class PedidoDeEntradaTests
{
    [Fact]
    public void R34_PedidoSemAprovacao_NaoEntraNaFamilia()
    {
        var familia = Familia.Criar("Família Silva", "Ana");

        var pedido = familia.ReceberPedidoDeEntrada(Guid.CreateVersion7(), "Bruno").Valor;

        Assert.Equal(SituacaoDoPedido.Pendente, pedido.Situacao);
        Assert.DoesNotContain(familia.Membros, m => m.Nome == "Bruno");
        Assert.Single(familia.Membros);
    }

    [Fact]
    public void R34_PedidoAprovadoPorAdultoDaFamilia_EntraComoAdulto()
    {
        var familia = Familia.Criar("Família Silva", "Ana");
        var ana = familia.Membros.Single();
        var pedido = familia.ReceberPedidoDeEntrada(Guid.CreateVersion7(), "Bruno").Valor;

        var resultado = familia.AprovarPedidoDeEntrada(pedido.Id, ana.Id);

        Assert.True(resultado.Sucesso);
        Assert.Equal("Bruno", resultado.Valor.Nome);
        Assert.Equal(PerfilDoMembro.Adulto, resultado.Valor.Perfil);
        Assert.Contains(resultado.Valor, familia.Membros);
        Assert.Equal(SituacaoDoPedido.Aprovado, pedido.Situacao);
        Assert.Equal(ana.Id, pedido.RespondidoPorId);
    }

    [Fact]
    public void R34_PedidoRecusado_NaoEntraNaFamilia()
    {
        var familia = Familia.Criar("Família Silva", "Ana");
        var ana = familia.Membros.Single();
        var pedido = familia.ReceberPedidoDeEntrada(Guid.CreateVersion7(), "Bruno").Valor;

        var resultado = familia.RecusarPedidoDeEntrada(pedido.Id, ana.Id);

        Assert.True(resultado.Sucesso);
        Assert.Equal(SituacaoDoPedido.Recusado, pedido.Situacao);
        Assert.Equal(ana.Id, pedido.RespondidoPorId);
        Assert.Single(familia.Membros);
    }

    [Fact]
    public void R34_CriancaNaoRespondePedidoDeEntrada()
    {
        var familia = Familia.Criar("Família Silva", "Ana");
        var crianca = familia.AdicionarCrianca(familia.Membros.Single().Id, "Lia", "lia").Valor;
        var pedido = familia.ReceberPedidoDeEntrada(Guid.CreateVersion7(), "Bruno").Valor;

        var aprovacao = familia.AprovarPedidoDeEntrada(pedido.Id, crianca.Id);
        var recusa = familia.RecusarPedidoDeEntrada(pedido.Id, crianca.Id);

        Assert.Equal("R34", aprovacao.CodigoDaRegra);
        Assert.Equal("R34", recusa.CodigoDaRegra);
        Assert.Equal(SituacaoDoPedido.Pendente, pedido.Situacao);
    }

    [Fact]
    public void R34_PedidoJaRespondido_NaoERespondidoDeNovo()
    {
        var familia = Familia.Criar("Família Silva", "Ana");
        var ana = familia.Membros.Single();
        var pedido = familia.ReceberPedidoDeEntrada(Guid.CreateVersion7(), "Bruno").Valor;
        familia.RecusarPedidoDeEntrada(pedido.Id, ana.Id);

        var resultado = familia.AprovarPedidoDeEntrada(pedido.Id, ana.Id);

        Assert.Equal("R34", resultado.CodigoDaRegra);
        Assert.Equal(SituacaoDoPedido.Recusado, pedido.Situacao);
        Assert.Single(familia.Membros);
    }

    [Fact]
    public void R34_MesmaContaComPedidoEsperando_NaoPedeDeNovo()
    {
        var familia = Familia.Criar("Família Silva", "Ana");
        var conta = Guid.CreateVersion7();
        familia.ReceberPedidoDeEntrada(conta, "Bruno");

        var resultado = familia.ReceberPedidoDeEntrada(conta, "Bruno");

        Assert.Equal("R34", resultado.CodigoDaRegra);
        Assert.Single(familia.PedidosDeEntrada);
    }

    [Fact]
    public void R34_DepoisDeRecusado_PodePedirDeNovo()
    {
        var familia = Familia.Criar("Família Silva", "Ana");
        var ana = familia.Membros.Single();
        var conta = Guid.CreateVersion7();
        var primeiro = familia.ReceberPedidoDeEntrada(conta, "Bruno").Valor;
        familia.RecusarPedidoDeEntrada(primeiro.Id, ana.Id);

        var resultado = familia.ReceberPedidoDeEntrada(conta, "Bruno");

        Assert.True(resultado.Sucesso);
        Assert.Equal(2, familia.PedidosDeEntrada.Count);
    }

    [Fact]
    public void Pedido_SemConta_EErroDeQuemChama()
    {
        var familia = Familia.Criar("Família Silva", "Ana");

        Assert.Throws<ArgumentException>(() => familia.ReceberPedidoDeEntrada(Guid.Empty, "Bruno"));
    }
}
