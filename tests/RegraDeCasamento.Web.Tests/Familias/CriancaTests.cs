using System.Net;
using System.Net.Http.Json;
using RegraDeCasamento.Shared.Acesso;
using RegraDeCasamento.Shared.Comum;
using RegraDeCasamento.Shared.Familias;

namespace RegraDeCasamento.Web.Tests.Familias;

public sealed class CriancaTests(AplicacaoDeTeste aplicacao) : IClassFixture<AplicacaoDeTeste>
{
    [Fact]
    public async Task R35_AdultoCriaACrianca_EElaEntraComApelidoEPin()
    {
        var (ana, familia) = await FamiliaNovaAsync();

        var criada = await CriarCriancaAsync(ana, "Lia Silva", "lia", "1234");

        Assert.Equal($"lia@{familia.Codigo}", criada.Usuario);

        var lia = aplicacao.CriarCliente();
        var login = await lia.PostAsJsonAsync("/api/acesso/entrar-crianca", new DadosDeLoginDaCrianca(familia.Codigo.ToLowerInvariant(), "lia", "1234"));
        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);

        var eu = await lia.EuAsync();
        Assert.Equal(PerfilDeAcesso.Crianca, eu.Perfil);
        Assert.Equal(familia.Id, eu.FamiliaId);
        Assert.Equal(criada.MembroId, eu.MembroId);

        var familiaDepois = await ana.GetFromJsonAsync<FamiliaDto>("/api/familia");
        var membro = Assert.Single(familiaDepois!.Membros, m => m.Perfil == PerfilDeAcesso.Crianca);
        Assert.Equal("lia", membro.Apelido);
    }

    [Fact]
    public async Task R4_CriancaCriada_NaoEntraNosEndpointsDeAdulto()
    {
        var (ana, familia) = await FamiliaNovaAsync();
        await CriarCriancaAsync(ana, "Lia", "lia", "1234");
        var lia = aplicacao.CriarCliente();
        await lia.PostAsJsonAsync("/api/acesso/entrar-crianca", new DadosDeLoginDaCrianca(familia.Codigo, "lia", "1234"));

        Assert.Equal(HttpStatusCode.Forbidden, (await lia.GetAsync("/api/familia")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await lia.PostAsJsonAsync("/api/familia/criancas", new DadosDaNovaCrianca("Theo", "theo", "1234"))).StatusCode);
    }

    [Fact]
    public async Task R35_ApelidoRepetidoNaFamilia_Recebe422()
    {
        var (ana, _) = await FamiliaNovaAsync();
        await CriarCriancaAsync(ana, "Lia", "lia", "1234");

        var resposta = await ana.PostAsJsonAsync("/api/familia/criancas", new DadosDaNovaCrianca("Outra Lia", "LIA", "5678"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        Assert.Equal("R35", (await resposta.Content.ReadFromJsonAsync<FalhaDeRegraDto>())!.CodigoDaRegra);
    }

    [Theory]
    [InlineData("123", "O PIN precisa ter de 4 a 6 números.")]
    [InlineData("1234567", "O PIN precisa ter de 4 a 6 números.")]
    [InlineData("12ab", "O PIN precisa ter de 4 a 6 números.")]
    public async Task PinInvalido_Recebe400_ENadaEGravado(string pin, string erroEsperado)
    {
        var (ana, _) = await FamiliaNovaAsync();

        var resposta = await ana.PostAsJsonAsync("/api/familia/criancas", new DadosDaNovaCrianca("Lia", "lia", pin));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Contains(erroEsperado, (await resposta.Content.ReadFromJsonAsync<ErrosDeDadosDto>())!.Erros);
        Assert.Single((await ana.GetFromJsonAsync<FamiliaDto>("/api/familia"))!.Membros);
    }

    [Fact]
    public async Task ApelidoComEspaco_Recebe400()
    {
        var (ana, _) = await FamiliaNovaAsync();

        var resposta = await ana.PostAsJsonAsync("/api/familia/criancas", new DadosDaNovaCrianca("Lia", "lia silva", "1234"));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task PinErrado_NaoEntra()
    {
        var (ana, familia) = await FamiliaNovaAsync();
        await CriarCriancaAsync(ana, "Lia", "lia", "1234");

        var resposta = await aplicacao.CriarCliente().PostAsJsonAsync("/api/acesso/entrar-crianca", new DadosDeLoginDaCrianca(familia.Codigo, "lia", "9999"));

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task MesmoApelidoEmOutraFamilia_EOutraCrianca()
    {
        var (ana, familiaDaAna) = await FamiliaNovaAsync();
        var (paula, familiaDaPaula) = await FamiliaNovaAsync();
        await CriarCriancaAsync(ana, "Lia", "lia", "1111");
        await CriarCriancaAsync(paula, "Lia", "lia", "2222");

        var liaDaPaula = aplicacao.CriarCliente();
        await liaDaPaula.PostAsJsonAsync("/api/acesso/entrar-crianca", new DadosDeLoginDaCrianca(familiaDaPaula.Codigo, "lia", "2222"));

        Assert.Equal(familiaDaPaula.Id, (await liaDaPaula.EuAsync()).FamiliaId);
        Assert.NotEqual(familiaDaAna.Id, familiaDaPaula.Id);
    }

    [Fact]
    public async Task Adulto_NaoPodeUsarPinComoSenha()
    {
        var resposta = await aplicacao.CriarCliente().PostAsJsonAsync("/api/acesso/cadastrar", new DadosDeCadastro(ClienteDaApi.EmailNovo(), "1234"));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    private async Task<(HttpClient Adulto, FamiliaDto Familia)> FamiliaNovaAsync()
    {
        var adulto = await aplicacao.CadastrarAsync();
        var familia = await adulto.CriarFamiliaAsync("Família Nova", "Ana");
        return (adulto, familia);
    }

    private static async Task<CriancaCriadaDto> CriarCriancaAsync(HttpClient adulto, string nome, string apelido, string pin)
    {
        var resposta = await adulto.PostAsJsonAsync("/api/familia/criancas", new DadosDaNovaCrianca(nome, apelido, pin));
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<CriancaCriadaDto>())!;
    }
}
