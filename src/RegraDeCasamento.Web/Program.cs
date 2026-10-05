using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RegraDeCasamento.Application.Acesso;
using RegraDeCasamento.Application.Comum;
using RegraDeCasamento.Application.Familias;
using RegraDeCasamento.Application.Financas;
using RegraDeCasamento.Application.Mercado;
using RegraDeCasamento.Infrastructure.Mercado;
using RegraDeCasamento.Application.Relatorios;
using RegraDeCasamento.Infrastructure.Relatorios;
using RegraDeCasamento.Infrastructure.Financas;
using RegraDeCasamento.Infrastructure.Acesso;
using RegraDeCasamento.Infrastructure.Familias;
using RegraDeCasamento.Infrastructure.Persistencia;
using RegraDeCasamento.Shared.Acesso;
using RegraDeCasamento.Web;
using RegraDeCasamento.Web.Acesso;
using RegraDeCasamento.Web.Client.Pages;
using RegraDeCasamento.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// Banco: este é o único lugar onde ele é configurado (STACK.md, regra 4).
// A senha fica fora do Git: veja a seção "Banco local" do STACK.md.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<UsuarioLogado>();
builder.Services.AddScoped<IUsuarioAtual>(servicos => servicos.GetRequiredService<UsuarioLogado>());
builder.Services.AddScoped<IFamiliaAtual>(servicos => servicos.GetRequiredService<UsuarioLogado>());
builder.Services.AddDbContext<RegraDeCasamentoDbContext>(opcoes =>
    opcoes.UseNpgsql(builder.Configuration.GetConnectionString("RegraDeCasamento")));
builder.Services.AddScoped<IUnidadeDeTrabalho>(servicos => servicos.GetRequiredService<RegraDeCasamentoDbContext>());

// Login: Identity com cookie, ligado ao Membro, com os perfis Adulto e Crianca (ARCHITECTURE.md, 5.2).
// A ordem importa: AddIdentityCookies cria os eventos do cookie, e CookieDeLogin os ajusta depois.
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();
builder.Services.ConfigureApplicationCookie(CookieDeLogin.Configurar);
builder.Services.AddIdentityCore<Conta>(opcoes =>
    {
        // O usuário da criança é "apelido@CODIGO", e o apelido pode ter acento (R35).
        opcoes.User.AllowedUserNameCharacters = "";

        // As regras de senha dependem do tipo de login (adulto: senha forte; criança: PIN).
        // Quem confere é o ValidadorDeSenha; aqui fica só o mínimo comum aos dois.
        opcoes.Password.RequiredLength = ValidadorDeSenha.TamanhoMinimoDoPin;
        opcoes.Password.RequireDigit = false;
        opcoes.Password.RequireLowercase = false;
        opcoes.Password.RequireUppercase = false;
        opcoes.Password.RequireNonAlphanumeric = false;
        opcoes.Password.RequiredUniqueChars = 1;
    })
    .AddEntityFrameworkStores<RegraDeCasamentoDbContext>()
    .AddSignInManager()
    .AddClaimsPrincipalFactory<FabricaDeClaims>()
    .AddPasswordValidator<ValidadorDeSenha>()
    .AddErrorDescriber<ErrosDoIdentityEmPortugues>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Politicas.Adulto, politica => politica.RequireClaim(ClaimsDeAcesso.Perfil, nameof(PerfilDeAcesso.Adulto)));

builder.Services.AddScoped<IRepositorioDeFamilias, RepositorioDeFamilias>();
builder.Services.AddScoped<IContasDeAcesso, ContasDeAcesso>();
builder.Services.AddScoped<CriarFamilia>();
builder.Services.AddScoped<VerMinhaFamilia>();
builder.Services.AddScoped<PedirEntradaNaFamilia>();
builder.Services.AddScoped<ResponderPedidoDeEntrada>();
builder.Services.AddScoped<AdicionarCrianca>();
builder.Services.AddScoped<IRepositorioDeFinancas, RepositorioDeFinancas>();
builder.Services.AddScoped<FinancasDaCasa>();
builder.Services.AddSingleton<IGeradorDePlanilhas, GeradorDePlanilhas>();
builder.Services.AddScoped<IRepositorioDeCompras, RepositorioDeCompras>();
builder.Services.AddScoped<MercadoDaFamilia>();
builder.Services.AddScoped<ListarPedidosDeEntradaPendentes>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapEndpointsDaApi();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(RegraDeCasamento.Web.Client._Imports).Assembly);

app.Run();
