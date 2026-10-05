using System.Net;
using System.Net.Http.Json;
using RegraDeCasamento.Shared.Acesso;
using RegraDeCasamento.Shared.Comum;
using RegraDeCasamento.Shared.Familias;

namespace RegraDeCasamento.Web.Tests.Familias;

public sealed class CriarFamiliaTests(AplicacaoDeTeste aplicacao) : IClassFixture<AplicacaoDeTeste>
{
    [Fact]
    public async Task CadastroECriacaoDaFamilia_JaEntramComoAdulto()
    {
        var cliente = await aplicacao.CadastrarAsync();

        var antes = await cliente.EuAsync();
        var criada = await cliente.CriarFamiliaAsync("Família Silva", "Ana");
        var depois = await cliente.EuAsync();
        var vista = await cliente.GetFromJsonAsync<FamiliaDto>("/api/familia");

        Assert.Null(antes.FamiliaId);
        Assert.Equal(PerfilDeAcesso.Adulto, depois.Perfil);
        Assert.Equal(criada.Id, depois.FamiliaId);
        Assert.Equal("Família Silva", vista!.Nome);
        Assert.Equal(criada.Codigo, vista.Codigo);
        Assert.Equal("Ana", Assert.Single(vista.Membros).Nome);
    }

    [Fact]
    public async Task R1_ContaQueJaTemFamilia_NaoCriaOutra()
    {
        var cliente = await aplicacao.CadastrarAsync();
        await cliente.CriarFamiliaAsync();

        var resposta = await cliente.PostAsJsonAsync("/api/familias", new DadosDaNovaFamilia("Outra", "Ana"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        Assert.Equal("R1", (await resposta.Content.ReadFromJsonAsync<FalhaDeRegraDto>())!.CodigoDaRegra);
    }

    [Fact]
    public async Task SemNome_Recebe400ComAMensagemEmPortugues()
    {
        var cliente = await aplicacao.CadastrarAsync();

        var resposta = await cliente.PostAsJsonAsync("/api/familias", new DadosDaNovaFamilia("  ", "Ana"));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal("Preencha o nome da família.", Assert.Single((await resposta.Content.ReadFromJsonAsync<ErrosDeDadosDto>())!.Erros));
    }

    [Fact]
    public async Task SemLogin_NaoCriaFamilia()
    {
        var resposta = await aplicacao.CriarCliente().PostAsJsonAsync("/api/familias", new DadosDaNovaFamilia("Família", "Ana"));

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task ContaSemFamilia_NaoEntraNosEndpointsDeAdulto()
    {
        var cliente = await aplicacao.CadastrarAsync();

        var resposta = await cliente.GetAsync("/api/familia");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Theory]
    [InlineData("nao-e-email", "Senha!123", "Informe um e-mail válido.")]
    [InlineData("valido@teste.com", "senha!123", "A senha precisa ter pelo menos uma letra maiúscula.")]
    public async Task Cadastro_ComDadoInvalido_Recebe400(string email, string senha, string erroEsperado)
    {
        var resposta = await aplicacao.CriarCliente().PostAsJsonAsync("/api/acesso/cadastrar", new DadosDeCadastro(email, senha));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Contains(erroEsperado, (await resposta.Content.ReadFromJsonAsync<ErrosDeDadosDto>())!.Erros);
    }

    [Fact]
    public async Task Cadastro_ComEmailRepetido_Recebe400()
    {
        var email = ClienteDaApi.EmailNovo();
        await aplicacao.CadastrarAsync(email);

        var resposta = await aplicacao.CriarCliente().PostAsJsonAsync("/api/acesso/cadastrar", new DadosDeCadastro(email, AplicacaoDeTeste.Senha));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }
}
