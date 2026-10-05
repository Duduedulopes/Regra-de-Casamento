using System.Net;
using System.Net.Http.Json;
using RegraDeCasamento.Shared.Acesso;
using RegraDeCasamento.Shared.Comum;
using RegraDeCasamento.Shared.Familias;

namespace RegraDeCasamento.Web.Tests.Familias;

public sealed class PedidoDeEntradaTests(AplicacaoDeTeste aplicacao) : IClassFixture<AplicacaoDeTeste>
{
    [Fact]
    public async Task R34_PedidoAprovado_QuemPediuViraOSegundoAdulto()
    {
        var (ana, familia) = await FamiliaNovaAsync();
        var bruno = await aplicacao.CadastrarAsync();

        var pedido = await PedirEntradaAsync(bruno, familia.Codigo, "Bruno");
        var pendentes = await ana.GetFromJsonAsync<List<PedidoDeEntradaDto>>("/api/familia/pedidos-de-entrada");
        var aprovacao = await ana.PostAsync($"/api/familia/pedidos-de-entrada/{pedido.PedidoId}/aprovar", null);
        await bruno.PostAsync("/api/acesso/atualizar", null);

        Assert.Equal("Bruno", Assert.Single(pendentes!).NomeDoSolicitante);
        Assert.Equal(HttpStatusCode.NoContent, aprovacao.StatusCode);

        var eu = await bruno.EuAsync();
        Assert.Equal(familia.Id, eu.FamiliaId);
        Assert.Equal(PerfilDeAcesso.Adulto, eu.Perfil);

        var familiaDepois = await ana.GetFromJsonAsync<FamiliaDto>("/api/familia");
        Assert.Equal(["Ana", "Bruno"], familiaDepois!.Membros.Select(membro => membro.Nome));
        Assert.Empty((await ana.GetFromJsonAsync<List<PedidoDeEntradaDto>>("/api/familia/pedidos-de-entrada"))!);
    }

    [Fact]
    public async Task R34_PedidoSemAprovacao_NaoEntraNaFamilia()
    {
        var (_, familia) = await FamiliaNovaAsync();
        var bruno = await aplicacao.CadastrarAsync();

        await PedirEntradaAsync(bruno, familia.Codigo, "Bruno");
        await bruno.PostAsync("/api/acesso/atualizar", null);

        Assert.Null((await bruno.EuAsync()).FamiliaId);
        Assert.Equal(HttpStatusCode.Forbidden, (await bruno.GetAsync("/api/familia")).StatusCode);
    }

    [Fact]
    public async Task R34_PedidoRecusado_NaoEntraNaFamilia()
    {
        var (ana, familia) = await FamiliaNovaAsync();
        var bruno = await aplicacao.CadastrarAsync();
        var pedido = await PedirEntradaAsync(bruno, familia.Codigo, "Bruno");

        var recusa = await ana.PostAsync($"/api/familia/pedidos-de-entrada/{pedido.PedidoId}/recusar", null);
        await bruno.PostAsync("/api/acesso/atualizar", null);

        Assert.Equal(HttpStatusCode.NoContent, recusa.StatusCode);
        Assert.Null((await bruno.EuAsync()).FamiliaId);
        Assert.Single((await ana.GetFromJsonAsync<FamiliaDto>("/api/familia"))!.Membros);
    }

    [Fact]
    public async Task R34_CodigoQueNaoExiste_NaoViraPedido()
    {
        var bruno = await aplicacao.CadastrarAsync();

        var resposta = await bruno.PostAsJsonAsync("/api/familias/entrar", new DadosDoPedidoDeEntrada("ZZZZZZZZ", "Bruno"));

        await AssertFalhaDeRegraAsync(resposta, "R34");
    }

    [Fact]
    public async Task R34_CodigoComMinusculasEEspacos_Funciona()
    {
        var (_, familia) = await FamiliaNovaAsync();
        var bruno = await aplicacao.CadastrarAsync();

        var resposta = await bruno.PostAsJsonAsync("/api/familias/entrar", new DadosDoPedidoDeEntrada($"  {familia.Codigo.ToLowerInvariant()} ", "Bruno"));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task R1_FamiliaComOsDoisAdultos_NaoRecebeMaisPedidos()
    {
        var (ana, familia) = await FamiliaNovaAsync();
        var bruno = await aplicacao.CadastrarAsync();
        var pedido = await PedirEntradaAsync(bruno, familia.Codigo, "Bruno");
        await ana.PostAsync($"/api/familia/pedidos-de-entrada/{pedido.PedidoId}/aprovar", null);
        var carla = await aplicacao.CadastrarAsync();

        var resposta = await carla.PostAsJsonAsync("/api/familias/entrar", new DadosDoPedidoDeEntrada(familia.Codigo, "Carla"));

        await AssertFalhaDeRegraAsync(resposta, "R1");
    }

    [Fact]
    public async Task R1_QuemJaTemFamilia_NaoPedeEntradaEmOutra()
    {
        var (ana, _) = await FamiliaNovaAsync();
        var (_, outraFamilia) = await FamiliaNovaAsync();

        var resposta = await ana.PostAsJsonAsync("/api/familias/entrar", new DadosDoPedidoDeEntrada(outraFamilia.Codigo, "Ana"));

        await AssertFalhaDeRegraAsync(resposta, "R1");
    }

    [Fact]
    public async Task R1_QuemPediuEEntrouEmOutraFamilia_NaoEAprovado()
    {
        var (ana, familia) = await FamiliaNovaAsync();
        var bruno = await aplicacao.CadastrarAsync();
        var pedido = await PedirEntradaAsync(bruno, familia.Codigo, "Bruno");
        await bruno.CriarFamiliaAsync("Família do Bruno", "Bruno");

        var resposta = await ana.PostAsync($"/api/familia/pedidos-de-entrada/{pedido.PedidoId}/aprovar", null);

        await AssertFalhaDeRegraAsync(resposta, "R1");
    }

    [Fact]
    public async Task R5_AdultoDeOutraFamilia_NaoRespondePedidoDestaFamilia()
    {
        var (_, familia) = await FamiliaNovaAsync();
        var (paula, _) = await FamiliaNovaAsync();
        var bruno = await aplicacao.CadastrarAsync();
        var pedido = await PedirEntradaAsync(bruno, familia.Codigo, "Bruno");

        var resposta = await paula.PostAsync($"/api/familia/pedidos-de-entrada/{pedido.PedidoId}/aprovar", null);

        await AssertFalhaDeRegraAsync(resposta, "R5");
    }

    [Fact]
    public async Task R4_CriancaNaoRespondePedidoDeEntrada()
    {
        var crianca = aplicacao.CriarCliente();
        await crianca.PostAsJsonAsync("/api/acesso/entrar", new DadosDeLogin(AplicacaoDeTeste.UsuarioCrianca, AplicacaoDeTeste.Senha));

        var resposta = await crianca.PostAsync($"/api/familia/pedidos-de-entrada/{Guid.CreateVersion7()}/aprovar", null);

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    private async Task<(HttpClient Adulto, FamiliaDto Familia)> FamiliaNovaAsync()
    {
        var adulto = await aplicacao.CadastrarAsync();
        var familia = await adulto.CriarFamiliaAsync("Família Nova", "Ana");
        return (adulto, familia);
    }

    private static async Task<PedidoEnviadoDto> PedirEntradaAsync(HttpClient cliente, string codigo, string nome)
    {
        var resposta = await cliente.PostAsJsonAsync("/api/familias/entrar", new DadosDoPedidoDeEntrada(codigo, nome));
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<PedidoEnviadoDto>())!;
    }

    private static async Task AssertFalhaDeRegraAsync(HttpResponseMessage resposta, string codigoEsperado)
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        Assert.Equal(codigoEsperado, (await resposta.Content.ReadFromJsonAsync<FalhaDeRegraDto>())!.CodigoDaRegra);
    }
}
