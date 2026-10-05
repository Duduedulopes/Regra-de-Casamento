using System.Net;
using System.Net.Http.Json;
using RegraDeCasamento.Shared.Acesso;
using RegraDeCasamento.Shared.Comum;
using RegraDeCasamento.Shared.Familias;
using RegraDeCasamento.Shared.Financas;
using RegraDeCasamento.Shared.Mercado;

namespace RegraDeCasamento.Web.Client.Servicos;

/// <summary>Resposta da API já traduzida para a tela: deu certo, ou as mensagens para mostrar.</summary>
public sealed record Resposta<T>(T? Valor, IReadOnlyList<string> Erros)
{
    public bool DeuCerto => Erros.Count == 0;
}

/// <summary>
/// Todas as chamadas que as telas fazem à API. As permissões são conferidas no servidor;
/// aqui só se transforma cada resposta numa mensagem que a família entende.
/// </summary>
public sealed class ApiDoApp(HttpClient http)
{
    public Task<Resposta<bool>> CadastrarAsync(DadosDeCadastro dados) =>
        EnviarAsync<bool>(() => http.PostAsJsonAsync("api/acesso/cadastrar", dados));

    public Task<Resposta<bool>> EntrarAsync(DadosDeLogin dados) =>
        EnviarAsync<bool>(() => http.PostAsJsonAsync("api/acesso/entrar", dados), "E-mail ou senha errados.");

    public Task<Resposta<bool>> EntrarCriancaAsync(DadosDeLoginDaCrianca dados) =>
        EnviarAsync<bool>(() => http.PostAsJsonAsync("api/acesso/entrar-crianca", dados), "Código, apelido ou PIN errados.");

    public Task<Resposta<bool>> SairAsync() =>
        EnviarAsync<bool>(() => http.PostAsync("api/acesso/sair", null));

    public Task<Resposta<bool>> AtualizarLoginAsync() =>
        EnviarAsync<bool>(() => http.PostAsync("api/acesso/atualizar", null));

    public async Task<MeuAcessoDto?> EuAsync()
    {
        var resposta = await http.GetAsync("api/acesso/eu");
        return resposta.IsSuccessStatusCode ? await resposta.Content.ReadFromJsonAsync<MeuAcessoDto>() : null;
    }

    public Task<Resposta<FamiliaDto>> CriarFamiliaAsync(DadosDaNovaFamilia dados) =>
        EnviarAsync<FamiliaDto>(() => http.PostAsJsonAsync("api/familias", dados));

    public Task<Resposta<PedidoEnviadoDto>> PedirEntradaAsync(DadosDoPedidoDeEntrada dados) =>
        EnviarAsync<PedidoEnviadoDto>(() => http.PostAsJsonAsync("api/familias/entrar", dados));

    public Task<Resposta<FamiliaDto>> MinhaFamiliaAsync() =>
        EnviarAsync<FamiliaDto>(() => http.GetAsync("api/familia"));

    public Task<Resposta<List<PedidoDeEntradaDto>>> PedidosDeEntradaAsync() =>
        EnviarAsync<List<PedidoDeEntradaDto>>(() => http.GetAsync("api/familia/pedidos-de-entrada"));

    public Task<Resposta<bool>> AprovarPedidoAsync(Guid pedidoId) =>
        EnviarAsync<bool>(() => http.PostAsync($"api/familia/pedidos-de-entrada/{pedidoId}/aprovar", null));

    public Task<Resposta<bool>> RecusarPedidoAsync(Guid pedidoId) =>
        EnviarAsync<bool>(() => http.PostAsync($"api/familia/pedidos-de-entrada/{pedidoId}/recusar", null));

    public Task<Resposta<CriancaCriadaDto>> AdicionarCriancaAsync(DadosDaNovaCrianca dados) =>
        EnviarAsync<CriancaCriadaDto>(() => http.PostAsJsonAsync("api/familia/criancas", dados));

    public Task<Resposta<List<DespesaDto>>> DespesasAsync(int ano, int mes) =>
        EnviarAsync<List<DespesaDto>>(() => http.GetAsync($"api/financas/despesas?ano={ano}&mes={mes}"));

    public Task<Resposta<DespesaDto>> LancarDespesaAsync(DadosDaNovaDespesa dados) =>
        EnviarAsync<DespesaDto>(() => http.PostAsJsonAsync("api/financas/despesas", dados));

    public Task<Resposta<DespesaDto>> PagarDespesaAsync(Guid despesaId, DadosDoPagamento dados) =>
        EnviarAsync<DespesaDto>(() => http.PostAsJsonAsync($"api/financas/despesas/{despesaId}/pagar", dados));

    public Task<Resposta<DespesaDto>> DesfazerPagamentoAsync(Guid despesaId) =>
        EnviarAsync<DespesaDto>(() => http.PostAsync($"api/financas/despesas/{despesaId}/desfazer-pagamento", null));

    public Task<Resposta<List<RendaDto>>> RendasAsync() =>
        EnviarAsync<List<RendaDto>>(() => http.GetAsync("api/financas/rendas"));

    public Task<Resposta<RendaDto>> LancarRendaAsync(DadosDaNovaRenda dados) =>
        EnviarAsync<RendaDto>(() => http.PostAsJsonAsync("api/financas/rendas", dados));

    public Task<Resposta<List<DividaDto>>> DividasAsync() =>
        EnviarAsync<List<DividaDto>>(() => http.GetAsync("api/financas/dividas"));

    public Task<Resposta<DividaDto>> LancarDividaAsync(DadosDaNovaDivida dados) =>
        EnviarAsync<DividaDto>(() => http.PostAsJsonAsync("api/financas/dividas", dados));

    public Task<Resposta<List<CompraDto>>> ComprasAsync(int ano, int mes) =>
        EnviarAsync<List<CompraDto>>(() => http.GetAsync($"api/mercado/compras?ano={ano}&mes={mes}"));

    public Task<Resposta<CompraDto>> LancarCompraAsync(DadosDaNovaCompra dados) =>
        EnviarAsync<CompraDto>(() => http.PostAsJsonAsync("api/mercado/compras", dados));

    public Task<Resposta<ResumoDoMercadoDto>> ResumoDoMercadoAsync(int ano, int mes) =>
        EnviarAsync<ResumoDoMercadoDto>(() => http.GetAsync($"api/mercado/resumo?ano={ano}&mes={mes}"));

    public static string EnderecoDaPlanilhaDoMercado(int ano, int mes) => $"api/mercado/planilha?ano={ano}&mes={mes}";

    public Task<Resposta<ContribuicaoDoMesDto>> ContribuicaoAsync(int ano, int mes) =>
        EnviarAsync<ContribuicaoDoMesDto>(() => http.GetAsync($"api/financas/contribuicao?ano={ano}&mes={mes}"));

    /// <summary>O endereço da planilha; o navegador baixa direto, com o cookie de login.</summary>
    public static string EnderecoDaPlanilhaDeContribuicao(int ano, int mes) => $"api/financas/contribuicao/planilha?ano={ano}&mes={mes}";

    private static async Task<Resposta<T>> EnviarAsync<T>(Func<Task<HttpResponseMessage>> chamada, string? mensagemDe401 = null)
    {
        HttpResponseMessage resposta;
        try
        {
            resposta = await chamada();
        }
        catch (HttpRequestException)
        {
            return Falha<T>("Sem conexão com o servidor. Confira a internet e tente de novo.");
        }

        if (resposta.IsSuccessStatusCode)
        {
            var temCorpo = resposta.StatusCode != HttpStatusCode.NoContent && typeof(T) != typeof(bool);
            var valor = temCorpo ? await resposta.Content.ReadFromJsonAsync<T>() : default;
            return new Resposta<T>(valor, []);
        }

        return resposta.StatusCode switch
        {
            HttpStatusCode.BadRequest when await LerAsync<ErrosDeDadosDto>(resposta) is { } erros => new Resposta<T>(default, erros.Erros),
            HttpStatusCode.UnprocessableEntity when await LerAsync<FalhaDeRegraDto>(resposta) is { } falha => Falha<T>(falha.Mensagem),
            HttpStatusCode.Unauthorized => Falha<T>(mensagemDe401 ?? "Você precisa entrar de novo."),
            HttpStatusCode.Forbidden => Falha<T>("Isto é só para os adultos da família."),
            _ => Falha<T>("Algo deu errado. Tente de novo."),
        };
    }

    private static Resposta<T> Falha<T>(string mensagem) => new(default, [mensagem]);

    private static async Task<TCorpo?> LerAsync<TCorpo>(HttpResponseMessage resposta)
    {
        try
        {
            return await resposta.Content.ReadFromJsonAsync<TCorpo>();
        }
        catch (System.Text.Json.JsonException)
        {
            return default;
        }
    }
}
