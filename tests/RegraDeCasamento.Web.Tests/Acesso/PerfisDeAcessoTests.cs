using System.Net;
using System.Net.Http.Json;
using RegraDeCasamento.Shared.Acesso;
using RegraDeCasamento.Shared.Familias;

namespace RegraDeCasamento.Web.Tests.Acesso;

public sealed class PerfisDeAcessoTests(AplicacaoDeTeste aplicacao) : IClassFixture<AplicacaoDeTeste>
{
    private const string EndpointDeAdulto = "/api/familia/pedidos-de-entrada";

    [Fact]
    public async Task R4_CriancaLogada_RecebeAcessoNegadoNumEndpointDeAdulto()
    {
        var cliente = await EntrarAsync(AplicacaoDeTeste.UsuarioCrianca);

        var resposta = await cliente.GetAsync(EndpointDeAdulto);

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        Assert.Empty(await resposta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task AdultoLogado_EntraNoEndpointDeAdulto()
    {
        var cliente = await EntrarAsync(AplicacaoDeTeste.UsuarioAdulto);

        var pedidos = await cliente.GetFromJsonAsync<List<PedidoDeEntradaDto>>(EndpointDeAdulto);

        var pedido = Assert.Single(pedidos!);
        Assert.Equal("Bruno", pedido.NomeDoSolicitante);
    }

    [Fact]
    public async Task SemLogin_RecebeNaoAutorizado_SemSerMandadoParaUmaPagina()
    {
        var cliente = aplicacao.CriarCliente();

        var resposta = await cliente.GetAsync(EndpointDeAdulto);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Null(resposta.Headers.Location);
    }

    [Fact]
    public async Task SenhaErrada_NaoEntra()
    {
        var cliente = aplicacao.CriarCliente();

        var resposta = await cliente.PostAsJsonAsync("/api/acesso/entrar", new DadosDeLogin(AplicacaoDeTeste.UsuarioCrianca, "errada"));

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/acesso/eu")).StatusCode);
    }

    [Theory]
    [InlineData(AplicacaoDeTeste.UsuarioAdulto, PerfilDeAcesso.Adulto)]
    [InlineData(AplicacaoDeTeste.UsuarioCrianca, PerfilDeAcesso.Crianca)]
    public async Task Login_GuardaOPerfilEAFamiliaDoMembro(string usuario, PerfilDeAcesso perfilEsperado)
    {
        var cliente = await EntrarAsync(usuario);

        var eu = await cliente.GetFromJsonAsync<MeuAcessoDto>("/api/acesso/eu");

        Assert.NotNull(eu);
        Assert.Equal(usuario, eu.Usuario);
        Assert.Equal(perfilEsperado, eu.Perfil);
        Assert.Equal(aplicacao.FamiliaId, eu.FamiliaId);
        Assert.NotNull(eu.MembroId);
    }

    [Fact]
    public async Task Sair_EncerraOLogin()
    {
        var cliente = await EntrarAsync(AplicacaoDeTeste.UsuarioAdulto);

        var resposta = await cliente.PostAsync("/api/acesso/sair", null);

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync(EndpointDeAdulto)).StatusCode);
    }

    [Fact]
    public async Task PaginaInicial_ContinuaAbrindoSemLogin()
    {
        var resposta = await aplicacao.CriarCliente().GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    private async Task<HttpClient> EntrarAsync(string usuario)
    {
        var cliente = aplicacao.CriarCliente();
        var resposta = await cliente.PostAsJsonAsync("/api/acesso/entrar", new DadosDeLogin(usuario, AplicacaoDeTeste.Senha));
        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        return cliente;
    }
}
