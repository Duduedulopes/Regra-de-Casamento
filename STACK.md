# STACK.md — Regra de Casamento

> Tecnologias e versões do projeto.
> Versões marcadas como **"fixar na instalação"** são anotadas aqui com o número exato no dia em que o pacote for instalado — nunca "a mais nova" solta.
> Última atualização: 02/10/2026

---

## Visão geral

| Camada | Tecnologia | Versão |
|---|---|---|
| Linguagem / plataforma | C# + **.NET 10 LTS** | 10.x (suporte até nov/2028) |
| Aplicação | **Blazor Web App** (um projeto, modo interativo WebAssembly) | .NET 10 |
| Instalável (celular e PC) | **PWA** adicionado à mão (manifesto + service worker) | — |
| Componentes visuais | **MudBlazor** (no Web.Client) | 9.11.0 |
| Tempo real (chat, checklist) | **SignalR** (já vem no ASP.NET Core) | .NET 10 |
| Login | **ASP.NET Core Identity** com **cookie** (`Microsoft.AspNetCore.Identity.EntityFrameworkCore`) | 10.0.12 |
| Banco de dados | **PostgreSQL** | 18.x (**ainda não instalado neste PC**) |
| Acesso ao banco | **EF Core** + `Npgsql.EntityFrameworkCore.PostgreSQL` | Npgsql 10.0.3 · EF Core 10.0.12 (`Microsoft.EntityFrameworkCore.Design`) |
| Migrations | ferramenta `dotnet-ef` (global, já estava instalada) | 10.0.10 |
| Planilhas (exportar) | **ClosedXML** (gera .xlsx, na Infrastructure) | 0.105.1 |
| Testes | **xUnit** + `xunit.runner.visualstudio` + `Microsoft.NET.Test.Sdk` + `coverlet.collector` (vieram do modelo `dotnet new xunit`) | 2.9.3 · 3.1.4 · 17.14.1 · 6.0.4 |
| Banco nos testes | **SQLite em memória** (`Microsoft.EntityFrameworkCore.Sqlite`), só nos projetos de teste | 10.0.12 |
| Testes do app inteiro | `Microsoft.AspNetCore.Mvc.Testing` (sobe o Program.cs de verdade) | 10.0.12 |
| Rede neural (treino) | **Python 3** (projeto Rede-Neural) | mesma versão do Rede-Neural |
| Rede neural (uso) | C# puro, modelo em **JSON** — mesma abordagem do Gerente | — |

## Ferramentas de desenvolvimento

| Ferramenta | Uso |
|---|---|
| **Visual Studio 2026** | IDE principal (o .NET 10 exige o VS 2026; VS Code também serve) |
| **pgAdmin 4** | Ver e consultar o banco (vem no instalador do PostgreSQL) |
| **Git + GitHub** | Versionamento (sem `Co-Authored-By` nem `Claude-Session` nos commits) |

## Decisões e o porquê

- **.NET 10 e não .NET 8:** o .NET 8 sai do suporte em 10/11/2026. Um sistema na internet com dados financeiros e conversas de família não pode rodar sem correções de segurança.
- **Blazor Web App único:** um projeto só para tela, API e SignalR. Escolha do dono do projeto.
- **Cookie e não JWT:** app e API estão no mesmo endereço, então o cookie do Identity é o padrão do modelo e é mais seguro no navegador (o token não fica exposto ao JavaScript). JWT só entra se um dia existir um cliente externo (ex.: app nativo).
- **PWA à mão:** o modelo Blazor Web App não traz a opção PWA pronta; adicionamos o manifesto e o service worker para instalar no celular e no PC.
- **PostgreSQL desde o início:** a publicação planejada é na Oracle Always Free (máquina ARM), onde o SQL Server não roda. Desenvolver no mesmo banco evita refazer migrations no fim.
- **Rede em Python, uso em C#:** "O Python é onde a rede nasce e é medida. O C# é onde ela vive e aprende." Sem servidor Python em produção.

## Regras técnicas que valem desde o primeiro commit

1. **Datas sempre em UTC** no banco (`DateTime.UtcNow`); conversão para o horário de Brasília só na tela.
2. **Ids gerados no C#** (Guid) com `ValueGeneratedNever()` — nada de `NEWID()`/`GETDATE()` ou funções de banco.
3. **Nada de SQL escrito à mão** (`FromSqlRaw`, procedures): tudo por LINQ.
4. **Configuração do banco num lugar só** (`Program.cs`).
5. **Segredos fora do Git** (senha do banco, chaves): `appsettings.Development.json` e *User Secrets* no `.gitignore`.

## Banco local (quando o PostgreSQL for instalado)

A conexão em `appsettings.json` não tem senha. A senha vai nos **User Secrets** (ficam fora da pasta do projeto). O comando abaixo grava a conexão completa:

```
dotnet user-secrets set "ConnectionStrings:RegraDeCasamento" "Host=localhost;Port=5432;Database=regra_de_casamento;Username=postgres;Password=SUA_SENHA" --project src/RegraDeCasamento.Web
```

Depois, para criar as tabelas:

```
dotnet ef database update --project src/RegraDeCasamento.Infrastructure --startup-project src/RegraDeCasamento.Web
```

Migrations existentes: `Inicial` (famílias, membros, pedidos de entrada), `Acesso` (contas de login do Identity) `PinDaCrianca` (tipo de login da conta: e-mail ou PIN) `Financas` (despesas e rendas) e `DividasEMercado` (dívidas com parcelas, compras e itens).

Desde 05/10/2026 o PC tem o **PostgreSQL 18** rodando (serviço `postgresql-x64-18`, porta 5432), com a senha nos User Secrets e o banco `regra_de_casamento` criado por todas as migrations. O pgAdmin 4 fica no menu Iniciar, em PostgreSQL 18.

## Publicação (ideia para quando o projeto finalizar — não fazer agora)

- Servidor: **Oracle Always Free** (ARM, 2 CPUs / 12 GB), conta **Pay As You Go** com **alerta de orçamento**, região São Paulo ou Vinhedo.
- Domínio: `.com.br` direto no **Registro.br** (~R$ 40/ano).
- HTTPS: **Let's Encrypt** (grátis).
- Backup do banco: obrigatório, definido na ARCHITECTURE.md.
