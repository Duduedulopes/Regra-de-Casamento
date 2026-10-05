using Microsoft.AspNetCore.Mvc;
using RegraDeCasamento.Web.Acesso;
using RegraDeCasamento.Web.Familias;
using RegraDeCasamento.Web.Financas;
using RegraDeCasamento.Web.Mercado;

namespace RegraDeCasamento.Web;

internal static class EndpointsDaApi
{
    public const string Prefixo = "/api";

    public static void MapEndpointsDaApi(this WebApplication app)
    {
        // A API responde só com o status (401, 403, 404...), sem cair na página "não encontrada" do Blazor.
        var api = app.MapGroup(Prefixo)
            .WithMetadata(new SkipStatusCodePagesAttribute())
            .AddEndpointFilter<FiltroDeDadosInvalidos>();

        api.MapEndpointsDeAcesso();
        api.MapEndpointsDasFamilias();
        api.MapEndpointsDaFamilia();
        api.MapEndpointsDeFinancas();
        api.MapEndpointsDoMercado();
    }
}
