using System.Net;
using System.Net.Http.Json;
using RegraDeCasamento.Shared.Acesso;
using RegraDeCasamento.Shared.Familias;

namespace RegraDeCasamento.Web.Tests;

/// <summary>Atalhos para os passos que vários testes repetem, sempre pela API, como o app fará.</summary>
internal static class ClienteDaApi
{
    public static string EmailNovo() => $"adulto.{Guid.NewGuid():N}@teste.com";

    /// <summary>Cadastra uma conta nova e devolve o cliente já logado nela.</summary>
    public static async Task<HttpClient> CadastrarAsync(this AplicacaoDeTeste aplicacao, string? email = null)
    {
        var cliente = aplicacao.CriarCliente();
        var resposta = await cliente.PostAsJsonAsync("/api/acesso/cadastrar", new DadosDeCadastro(email ?? EmailNovo(), AplicacaoDeTeste.Senha));
        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        return cliente;
    }

    public static async Task<FamiliaDto> CriarFamiliaAsync(this HttpClient cliente, string nome = "Família Nova", string seuNome = "Ana")
    {
        var resposta = await cliente.PostAsJsonAsync("/api/familias", new DadosDaNovaFamilia(nome, seuNome));
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<FamiliaDto>())!;
    }

    public static async Task<MeuAcessoDto> EuAsync(this HttpClient cliente) =>
        (await cliente.GetFromJsonAsync<MeuAcessoDto>("/api/acesso/eu"))!;

    /// <summary>Uma família inteira pela API: Ana cria, Bruno entra pelo código e Lia é criada pela Ana. Todos logados.</summary>
    public static async Task<FamiliaDeTeste> FamiliaCompletaAsync(this AplicacaoDeTeste aplicacao)
    {
        var ana = await aplicacao.CadastrarAsync();
        var familia = await ana.CriarFamiliaAsync("Família Silva", "Ana");

        var bruno = await aplicacao.CadastrarAsync();
        var pedido = await bruno.PostAsJsonAsync("/api/familias/entrar", new DadosDoPedidoDeEntrada(familia.Codigo, "Bruno"));
        var pedidoId = (await pedido.Content.ReadFromJsonAsync<PedidoEnviadoDto>())!.PedidoId;
        Assert.Equal(HttpStatusCode.NoContent, (await ana.PostAsync($"/api/familia/pedidos-de-entrada/{pedidoId}/aprovar", null)).StatusCode);
        await bruno.PostAsync("/api/acesso/atualizar", null);

        var criada = await ana.PostAsJsonAsync("/api/familia/criancas", new DadosDaNovaCrianca("Lia", "lia", "1234"));
        Assert.Equal(HttpStatusCode.Created, criada.StatusCode);
        var lia = aplicacao.CriarCliente();
        await lia.PostAsJsonAsync("/api/acesso/entrar-crianca", new DadosDeLoginDaCrianca(familia.Codigo, "lia", "1234"));

        return new FamiliaDeTeste(
            ana,
            bruno,
            lia,
            (await ana.EuAsync()).MembroId!.Value,
            (await bruno.EuAsync()).MembroId!.Value,
            (await lia.EuAsync()).MembroId!.Value);
    }
}

internal sealed record FamiliaDeTeste(HttpClient Ana, HttpClient Bruno, HttpClient Lia, Guid AnaId, Guid BrunoId, Guid LiaId);
