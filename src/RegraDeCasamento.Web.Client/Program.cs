using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using RegraDeCasamento.Web.Client.Servicos;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// A API está no mesmo endereço do app, então o navegador manda o cookie de login sozinho.
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<ApiDoApp>();
builder.Services.AddScoped<Sessao>();
builder.Services.AddMudServices();

await builder.Build().RunAsync();
