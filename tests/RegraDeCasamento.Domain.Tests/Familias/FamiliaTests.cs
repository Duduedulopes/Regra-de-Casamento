using RegraDeCasamento.Domain.Comum;
using RegraDeCasamento.Domain.Familias;

namespace RegraDeCasamento.Domain.Tests.Familias;

public class FamiliaTests
{
    [Fact]
    public void R1_FamiliaNasceComOAdultoQueACriou()
    {
        var familia = Familia.Criar("Família Silva", "Ana");

        var adulto = Assert.Single(familia.Membros);
        Assert.Equal("Ana", adulto.Nome);
        Assert.Equal(PerfilDoMembro.Adulto, adulto.Perfil);
        Assert.Null(adulto.Apelido);
    }

    [Fact]
    public void R1_FamiliaComOsDoisAdultos_NaoRecebePedidoDeEntrada()
    {
        var familia = FamiliaComOsDoisAdultos();

        var resultado = familia.ReceberPedidoDeEntrada(Guid.CreateVersion7(), "Carla");

        Assert.Equal("R1", resultado.CodigoDaRegra);
        Assert.DoesNotContain(familia.PedidosDeEntrada, p => p.Situacao == SituacaoDoPedido.Pendente);
    }

    [Fact]
    public void R1_TerceiroAdulto_NaoEntraNaFamilia()
    {
        var familia = Familia.Criar("Família Silva", "Ana");
        var ana = familia.Membros.Single();
        var pedidoDoBruno = familia.ReceberPedidoDeEntrada(Guid.CreateVersion7(), "Bruno").Valor;
        var pedidoDaCarla = familia.ReceberPedidoDeEntrada(Guid.CreateVersion7(), "Carla").Valor;
        familia.AprovarPedidoDeEntrada(pedidoDoBruno.Id, ana.Id);

        var resultado = familia.AprovarPedidoDeEntrada(pedidoDaCarla.Id, ana.Id);

        Assert.Equal("R1", resultado.CodigoDaRegra);
        Assert.Equal(Familia.MaximoDeAdultos, familia.Membros.Count(m => m.Perfil == PerfilDoMembro.Adulto));
        Assert.Equal(SituacaoDoPedido.Pendente, pedidoDaCarla.Situacao);
    }

    [Fact]
    public void R1_CriancasNaoContamNoLimiteDeAdultos()
    {
        var familia = FamiliaComOsDoisAdultos();
        var ana = familia.Membros.First();

        var primeira = familia.AdicionarCrianca(ana.Id, "Lia", "lia");
        var segunda = familia.AdicionarCrianca(ana.Id, "Theo", "theo");

        Assert.True(primeira.Sucesso);
        Assert.True(segunda.Sucesso);
        Assert.Equal(4, familia.Membros.Count);
    }

    [Fact]
    public void R5_MembrosEPedidosLevamOFamiliaIdDaFamilia()
    {
        var familia = Familia.Criar("Família Silva", "Ana");
        var ana = familia.Membros.Single();
        var crianca = familia.AdicionarCrianca(ana.Id, "Lia", "lia").Valor;
        var pedido = familia.ReceberPedidoDeEntrada(Guid.CreateVersion7(), "Bruno").Valor;

        Assert.Equal(familia.Id, ana.FamiliaId);
        Assert.Equal(familia.Id, crianca.FamiliaId);
        Assert.Equal(familia.Id, pedido.FamiliaId);
    }

    [Fact]
    public void R5_AdultoDeOutraFamilia_NaoRespondePedidos()
    {
        var familia = Familia.Criar("Família Silva", "Ana");
        var pedido = familia.ReceberPedidoDeEntrada(Guid.CreateVersion7(), "Bruno").Valor;
        var adultoDeOutraFamilia = Familia.Criar("Família Souza", "Paula").Membros.Single();

        var aprovacao = familia.AprovarPedidoDeEntrada(pedido.Id, adultoDeOutraFamilia.Id);
        var recusa = familia.RecusarPedidoDeEntrada(pedido.Id, adultoDeOutraFamilia.Id);

        Assert.Equal("R5", aprovacao.CodigoDaRegra);
        Assert.Equal("R5", recusa.CodigoDaRegra);
        Assert.Equal(SituacaoDoPedido.Pendente, pedido.Situacao);
        Assert.Single(familia.Membros);
    }

    [Fact]
    public void R5_PedidoDeOutraFamilia_NaoERespondidoAqui()
    {
        var familia = Familia.Criar("Família Silva", "Ana");
        var ana = familia.Membros.Single();
        var outraFamilia = Familia.Criar("Família Souza", "Paula");
        var pedidoDaOutra = outraFamilia.ReceberPedidoDeEntrada(Guid.CreateVersion7(), "Bruno").Valor;

        var resultado = familia.AprovarPedidoDeEntrada(pedidoDaOutra.Id, ana.Id);

        Assert.Equal("R5", resultado.CodigoDaRegra);
        Assert.Equal(SituacaoDoPedido.Pendente, pedidoDaOutra.Situacao);
    }

    [Fact]
    public void R5_AdultoDeOutraFamilia_NaoCriaCriancaAqui()
    {
        var familia = Familia.Criar("Família Silva", "Ana");
        var adultoDeOutraFamilia = Familia.Criar("Família Souza", "Paula").Membros.Single();

        var resultado = familia.AdicionarCrianca(adultoDeOutraFamilia.Id, "Lia", "lia");

        Assert.Equal("R5", resultado.CodigoDaRegra);
        Assert.Single(familia.Membros);
    }

    [Theory]
    [InlineData(typeof(Membro))]
    [InlineData(typeof(PedidoDeEntrada))]
    public void R5_DadosDaFamiliaPertencemAUmaFamilia(Type tipo)
    {
        Assert.True(typeof(IPertenceAFamilia).IsAssignableFrom(tipo));
    }

    [Fact]
    public void Codigo_TemOitoLetrasFaceisDeDigitar()
    {
        var familia = Familia.Criar("Família Silva", "Ana");

        Assert.Equal(Familia.TamanhoDoCodigo, familia.Codigo.Length);
        Assert.DoesNotContain(familia.Codigo, c => "ILO01".Contains(c));
    }

    [Theory]
    [InlineData("", "Ana")]
    [InlineData("   ", "Ana")]
    [InlineData("Família Silva", "")]
    public void Criar_SemNome_EErroDeQuemChama(string nome, string nomeDoAdulto)
    {
        Assert.ThrowsAny<ArgumentException>(() => Familia.Criar(nome, nomeDoAdulto));
    }

    [Fact]
    public void Criar_GuardaOsNomesSemEspacosNasPontas()
    {
        var familia = Familia.Criar("  Família Silva ", " Ana ");

        Assert.Equal("Família Silva", familia.Nome);
        Assert.Equal("Ana", familia.Membros.Single().Nome);
    }

    private static Familia FamiliaComOsDoisAdultos()
    {
        var familia = Familia.Criar("Família Silva", "Ana");
        var ana = familia.Membros.Single();
        var pedido = familia.ReceberPedidoDeEntrada(Guid.CreateVersion7(), "Bruno").Valor;
        familia.AprovarPedidoDeEntrada(pedido.Id, ana.Id);
        return familia;
    }
}
