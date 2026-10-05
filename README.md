# Regra-de-Casamento
Sistema da família para combinar as regras do casal: finanças da casa e de cada um, mercado, tarefas com ranking, chat e um agente pessoal para cada membro. Feito em .NET 10, com Blazor e PostgreSQL

# ARCHITECTURE.md — Regra de Casamento

> Estrutura do projeto e quem depende de quem.
> Regras do domínio: `BUSINESS_RULES.md` · Tecnologias: `STACK.md` · Feature atual: `SPEC.md`
> Última atualização: 02/10/2026

---

## 1. Visão geral

```
 Celular / PC (cada membro da família)
 ┌──────────────────────────────────────┐
 │  RegraDeCasamento.Web.Client         │  Blazor WASM + PWA (instalável)
 │  telas, MudBlazor                    │
 └──────────────┬───────────────────────┘
                │ HTTPS (API)  +  SignalR (tempo real)
 Servidor       ▼
 ┌──────────────────────────────────────┐
 │  RegraDeCasamento.Web                │  Blazor Web App (servidor)
 │  endpoints da API · Hubs SignalR     │  login (Identity + cookie)
 │  agentes (adulto / criança)          │
 └──────────────┬───────────────────────┘
                ▼
 ┌──────────────────────────────────────┐
 │  RegraDeCasamento.Application        │  casos de uso
 └───────┬──────────────────────┬───────┘
         ▼                      ▼
 ┌───────────────┐      ┌──────────────────────────────┐
 │  Domain       │      │  Infrastructure              │
 │  entidades e  │◄─────│  EF Core + PostgreSQL        │
 │  regras puras │      │  planilhas (ClosedXML)       │
 └───────────────┘      └──────────────────────────────┘

 Cérebro (pasta própria, compartilhada com outro projeto)
 ┌──────────────────────────────────────┐
 │  Gerente (biblioteca C#)             │  classificador, Aprendiz, perfis
 └──────────────────────────────────────┘
           ▲ modelo JSON
 ┌──────────────────────────────────────┐
 │  Rede-Neural (Python)                │  treina e mede — só no PC de desenvolvimento
 └──────────────────────────────────────┘
```

---

## 2. Estrutura de pastas

Os documentos ficam **na raiz** (decisão do dono do projeto: mais fácil de achar).

```
Regra_de_Casamento/
├── ARCHITECTURE.md
├── BUSINESS_RULES.md
├── SPEC.md
├── STACK.md
├── .gitignore
├── RegraDeCasamento.sln
├── src/
│   ├── RegraDeCasamento.Domain/
│   ├── RegraDeCasamento.Application/
│   ├── RegraDeCasamento.Infrastructure/
│   ├── RegraDeCasamento.Shared/
│   ├── RegraDeCasamento.Web/
│   └── RegraDeCasamento.Web.Client/
└── tests/
    ├── RegraDeCasamento.Domain.Tests/
    ├── RegraDeCasamento.Application.Tests/
    ├── RegraDeCasamento.Infrastructure.Tests/   ← Base 4: isolamento entre famílias (a revisar)
    └── RegraDeCasamento.Web.Tests/              ← Base 5: login e perfis (a revisar)
```

Dentro de cada projeto, o que é comum a todos os módulos fica em `Comum/`; o login fica em `Acesso/`; o banco fica em `Infrastructure/Persistencia/` (com as migrations em `Persistencia/Migracoes/`).

---

## 3. Os projetos e o que cada um faz

| Projeto | Responsabilidade | Depende de |
|---|---|---|
| **Domain** | Entidades e regras puras (ex.: "troca só vale com aceite", "ponto nunca vira dinheiro"). Sem EF, sem ASP.NET. | nada |
| **Shared** | DTOs, enums e contratos que a tela e o servidor usam (ex.: `PerfilDeAcesso`, `TipoRenda`). | nada |
| **Application** | Casos de uso: lançar compra, pedir/aceitar troca, calcular ranking, comparar estudo planejado × realizado. Define interfaces (ex.: `IRepositorioDespesas`). | Domain, Shared |
| **Infrastructure** | Implementa as interfaces: `DbContext` (EF Core + PostgreSQL), migrations, exportação de planilha. | Application, Domain |
| **Web** | Servidor do Blazor Web App: endpoints da API, Hubs do SignalR, Identity, os dois agentes. **Onde as permissões são conferidas.** | Application, Infrastructure, Shared, Web.Client¹, Gerente² |
| **Web.Client** | O que roda no aparelho (WASM + PWA): telas e chamadas à API. **Nunca** acessa o banco direto. | Shared |

¹ Exigida pelo modelo do Blazor: é por ela que o servidor entrega o WebAssembly ao aparelho. Aprovada na Base 1.
² Entra quando o Gerente tiver a pasta própria (ver seção 9).

**Regra de ouro das dependências:** as setas só apontam para dentro. O Domain não conhece ninguém; a tela não conhece o banco.

### Projetos de teste

| Projeto | Testa | Depende de |
|---|---|---|
| **Domain.Tests** | Regras puras (um teste por regra, com o código no nome: `R34_...`) | Domain |
| **Application.Tests** | Casos de uso, com repositórios falsos | Application |
| **Infrastructure.Tests** | Filtro por família no `DbContext` (R5), com SQLite em memória | Infrastructure |
| **Web.Tests** | O app de verdade (login, perfis, policies), com SQLite em memória | Web |

---

## 4. Módulos

Cada módulo tem sua pasta dentro de Domain, Application e Web (ex.: `Domain/Tarefas/`, `Application/Tarefas/`).

| Módulo | Regras | Quem acessa |
|---|---|---|
| **Familias** — cadastro, pedido de entrada, aprovação, contas das crianças | R1–R5, R34, R35 | adultos (crianças só veem o próprio perfil) |
| **Financas** — rendas, despesas, dono, pago/a pagar, contribuição de cada um, cenários | R6–R11 | **só adultos** |
| **Mercado** — compras detalhadas, histórico, planilha | R12, R13 | **só adultos** |
| **Habitos** — café, refrigerante, academia | R14, R15 | **só adultos** |
| **Tarefas** — checklist, responsável, pontos, troca, ranking, mural, recompensa | R16–R22 | todos |
| **Estudo** — horário planejado, registro realizado, conferência | R31–R33 | crianças registram; adultos conferem |
| **Agenda** — tempo da família, do casal, turnos | R23 | adultos (crianças veem os próprios horários) |
| **Chat** — conversas individuais e grupos, tempo real, histórico | R24–R26, R3 | todos (chat das crianças privado) |
| **Agente** — agente dos adultos e agente das crianças | R27–R30 | conforme o perfil |
| **Relatorios** — dashboard e exportação de planilhas | R13 | **só adultos** |

---

## 5. Segurança e isolamento

### 5.1 Isolamento entre famílias (R5)
- **Toda** entidade que pertence a uma família tem `FamiliaId`.
- O `DbContext` aplica um **filtro global** (`HasQueryFilter`) com o `FamiliaId` do usuário logado. Nenhuma consulta precisa lembrar do `Where` — e nenhuma consegue esquecer.
- O filtro vale automaticamente para toda entidade que implementa `IPertenceAFamilia`. A `Familia` só enxerga a si mesma. Sem login (ou sem família), nada aparece.
- O `FamiliaId` vem do cookie de login (`IFamiliaAtual`, implementado no Web).
- **Exceções ao filtro (só estas):** qualquer outro uso de `IgnoreQueryFilters()` precisa de motivo escrito aqui.
  1. No login, a `FabricaDeClaims` busca o membro da própria conta, porque ainda não há família no contexto.
  2. No pedido de entrada (R34), o `RepositorioDeFamilias.ObterPeloCodigoAsync` acha a família pelo código, porque quem pede ainda não é dela. Nada dessa família volta para quem pediu; ele só recebe o Id do pedido.
  3. No agente, quando a LLM termina depois da requisição, o `HistoricoNoBanco.Trocar` grava o texto melhorado num escopo sem login. Ele só acha a mensagem pelo Id **e** pelo `MembroId` e `FamiliaId` do dono.
- Mesma ideia de "tenant" usada em sistemas com vários clientes.

### 5.2 Perfis de acesso (R2–R4)
- Dois perfis: **Adulto** e **Crianca** (claims do Identity).
- A conta de login (`Conta`) aponta para o `Membro`; um membro tem no máximo uma conta (R1, índice único no banco). No login, o cookie recebe as claims `perfil`, `familia_id` e `membro_id` (nomes em `Shared/Acesso/ClaimsDeAcesso`).
- Os módulos marcados "só adultos" ficam atrás de uma **policy de autorização** no servidor (`[Authorize(Policy = "Adulto")]`, ou `RequireAuthorization(Politicas.Adulto)` nos endpoints).
- Na API (`/api/...`), quem não está logado recebe **401** e quem não tem permissão recebe **403**, sem redirecionamento. Regra que não deixa recebe **422** (com o código da regra) e dado inválido recebe **400**.
- Dois tipos de login na `Conta`: adulto com **e-mail e senha forte**; criança com **`apelido@CODIGO` e PIN de 4 a 6 números** (R35). As regras de cada um ficam no `ValidadorDeSenha`.
- Os casos de uso que mexem na família e nas contas gravam tudo de uma vez (`IUnidadeDeTrabalho`), para nunca ficar uma conta sem membro ou um membro sem conta pela metade.
- **A permissão é conferida sempre no servidor (Web).** Esconder um botão na tela não é segurança — o código do Web.Client roda no aparelho e pode ser alterado.

### 5.3 Chat privado das crianças (R3)
- Uma conversa em que **todos os participantes são crianças** não aparece nas consultas feitas por adultos. A regra fica no caso de uso do Chat (Application), coberta por teste.

### 5.4 Dados sensíveis
- Senhas: hash do Identity. Segredos (senha do banco): fora do Git.
- HTTPS obrigatório. Em produção: backup automático do banco (ver seção 8).

---

## 6. Tempo real (SignalR)

| Hub | Para quê |
|---|---|
| **ChatHub** | Mensagens individuais e de grupo, "digitando...", entregue/lido |
| **FamiliaHub** | Avisos ao vivo: tarefa marcada, pedido de troca, pedido de entrada na família, novo TOP 1 |

Cada família é um **grupo do SignalR** (`familia-{FamiliaId}`); cada conversa também. Uma mensagem nunca sai do grupo da própria família.

---

## 7. Agentes e o cérebro

### 7.1 Fluxo (R29)
```
"lancei 85 no mercado"
   → Web: identifica quem fala (Adulto/Crianca) e a família
   → Gerente: a rede classifica a intenção  (lancar_compra, 94%)
   → Web: confere a PERMISSÃO do perfil       (Crianca → recusa, sem dado)
   → Application: monta a ação e PEDE CONFIRMAÇÃO
   → usuário confirma → grava → avisa pelo FamiliaHub
```
- O agente roda **no servidor**, porque a permissão (R28) e o aprendizado de cada família (R30) precisam ficar lá.
- Abaixo do limiar de confiança, o agente **pergunta de volta com botões** (como o Gerente já faz).

### 7.2 Dois perfis, um cérebro (R27, R28)
- `PerfilDeQuemFala.Adulto` e `PerfilDeQuemFala.Crianca`, cada um com sua **lista de permissão de intenções** — mesmo mecanismo do `PerfilDeQuemFala` do Gerente.

### 7.3 O Gerente como peça independente
- **Decisão:** a biblioteca do Gerente sai do projeto onde nasceu e vai para uma **pasta própria**, usada pelos dois projetos.
- O outro projeto passa a apontar para o novo lugar (feito numa etapa separada).

### 7.4 Treino (R30)
- **Modelo base:** treinado no Python (Rede-Neural) com as intenções do domínio da família; exportado em JSON e incluído no servidor.
- **Aprendizado de cada família:** o que a família ensina (correções pelos botões) é salvo **no banco, com `FamiliaId`**, e nunca se mistura com outra família. O modelo base nunca é sobrescrito.

---

## 8. Ambientes

| Ambiente | Onde | Banco |
|---|---|---|
| **Desenvolvimento** | PC do dono do projeto | PostgreSQL local |
| **Produção** (quando o projeto finalizar) | Oracle Always Free (ARM) + domínio `.com.br` + HTTPS Let's Encrypt | PostgreSQL no servidor, com backup automático |

---

## 9. Pendências de arquitetura

- [ ] **Nome e local da pasta própria do Gerente** (ex.: `..\Gerente\`) e se o projeto mantém o nome atual ou muda.
- [ ] **Quando** fazer a extração do Gerente (antes da primeira feature com agente — não bloqueia o cadastro da família).
- [ ] Estratégia de backup em produção (frequência e onde guardar).

# BUSINESS_RULES.md — Regra de Casamento

> Regras do domínio. Valem para **qualquer família** que usar o sistema.
> Toda regra tem um código (R1, R2...) para ser citada no código, nos testes e nas conversas.
> Última atualização: 02/10/2026

---

## Glossário

| Termo | Significado |
|---|---|
| **Família** | O grupo que usa o sistema junto: um casal + dependentes. Cada família é isolada das outras. |
| **Adulto** | Cada pessoa do casal. |
| **Criança** | Dependente com login próprio (filhos ou outros dependentes). |
| **Dono** | A quem pertence uma renda ou despesa: a casa, um adulto ou o outro. |
| **Planejado × Realizado** | O que estava previsto (ex.: horário de estudo) comparado com o que de fato aconteceu. |
| **TOP 1** | O membro da família do mês. |

---

## 1. Família e acesso

- **R1.** Uma família tem um casal (2 adultos) e dependentes. Cada membro tem o próprio login.
- **R2.** Os adultos veem **tudo** um do outro, sempre.
- **R3.** Os adultos veem os dados dos filhos (tarefas, estudo, pontos, histórico), **exceto o chat das crianças, que é privado**.
- **R4.** As crianças acessam **somente**: tarefas, horário de estudo, ranking, mural, o chat e o agente delas. **Nunca** veem contas, rendas, pensões ou gastos.
- **R5.** Uma família nunca enxerga os dados de outra família.
- **R34.** Um adulto só entra numa família que já existe por um **pedido de entrada**, feito com o **código da família**. O pedido precisa ser **aprovado por um adulto da família**; sem aprovação, ninguém entra. Cada pedido é respondido uma vez só.
- **R35.** A conta de uma criança é criada **por um adulto da família**. A criança não precisa de e-mail: ela entra com o **apelido** (único dentro da família), o **código da família** e um **PIN de 4 a 6 números**.

> R34 e R35 eram citadas no SPEC.md e na ARCHITECTURE.md, mas não estavam escritas aqui. O texto foi proposto pelo Claude e aceito em 02/10/2026 pelas recomendações (o dono do projeto pediu para seguir). Ainda vale uma leitura sua.

## 2. Dinheiro

- **R6.** Toda renda e toda despesa tem um **dono**: da casa (nossa), de um adulto, ou do outro.
  - Ex.: aluguel = da casa · carro = de um adulto · curso = do outro.
- **R7.** Cada renda tem um **tipo** e um **dia de recebimento**:
  - fixa (ex.: salário no 5º dia útil)
  - variável (ex.: comissão)
  - temporária, com data de fim (ex.: valor recebido por 4 meses)
  - pensão recebida
  - benefício de uso restrito (ex.: vale-alimentação, só para comida)
- **R8.** As contas da casa são pagas juntos. O sistema registra **quem pagou o quê**.
- **R9.** **Não existe dívida entre o casal.** O sistema mostra a **contribuição de cada um** (ex.: "Adulto A pagou 45% das contas da casa no mês") **apenas como registro histórico**, nunca como cobrança.
- **R10.** Cada conta tem status: **paga** ou **a pagar**.
- **R37.** *(regra nova em 05/10/2026, a revisar)* **Cada adulto mexe só no que é seu.** Contas, rendas, compras e dívidas que têm um adulto como dono são lançadas e alteradas **só por esse adulto**; o outro **vê tudo** (R2), mas não altera. O que é **da casa**, os dois lançam e alteram.
- **R38.** *(regra nova em 05/10/2026, a revisar)* **Dívidas** (cartão de crédito, empréstimo, compra parcelada) têm **dono** (R6), valor total, **número de parcelas** e o primeiro vencimento. Cada parcela vira uma **conta a pagar** no mês dela, e o sistema mostra quantas parcelas já foram pagas e quanto falta. São dívidas com bancos, lojas e cartões; **entre o casal não existe dívida** (R9).
- **R11.** Cenários e previsões (ex.: "e se a gente trocar de carro?") são **simulações** e nunca alteram os dados reais.

## 3. Mercado e hábitos

- **R12.** Cada compra é detalhada: **o que** foi comprado, **quem** comprou e **para quê**:
  - suprimento da casa
  - lanche / pedido para todos
  - pessoal
- **R13.** Tudo fica no histórico e gera a **planilha de controle** do casal.
- **R14.** Hábitos (ex.: café, refrigerante) têm controle próprio de **quantidade** e **custo**.
- **R15.** Atividades em comum (ex.: academia) são despesa do casal e têm **horário em comum** na agenda.

## 4. Tarefas, pontos e reconhecimento

> Tarefas ficam **separadas do dinheiro**. Ponto nunca vira dinheiro.

- **R16.** Cada tarefa tem um **responsável** e entra no **checklist do dia**: feito / falta fazer.
- **R17.** Cada tarefa cumprida vale **1 ponto**.
- **R18.** **Troca de função:** quem pede **paga pontos**, e o outro **precisa aceitar**. Sem aceite, nada muda.
- **R19.** O **membro da família do mês** (TOP 1) é decidido pela **porcentagem das tarefas cumpridas**, não pelo total de pontos — justo para quem não mora o tempo todo na casa.
- **R20.** O TOP 1 vai para o **mural**, que guarda todos os TOP 1 dos meses anteriores.
- **R21.** Recompensa: o TOP 1 escolhe o **lanche de sábado à noite** (ou outro dia combinado).
- **R22.** Pontos **nunca** viram dinheiro.

## 5. Estudo (crianças)

- **R31.** Cada criança tem um **horário de estudo planejado** (dias e horas).
- **R32.** A criança **registra o estudo realizado** (início e fim) — pelo app ou conversando com o agente ("comecei a estudar", "terminei").
- **R33.** O sistema compara **planejado × realizado**, e os **adultos conferem** se bate.

## 6. Tempo

- **R23.** A agenda tem: tempo da família toda, tempo do casal e horários de estudo, respeitando os turnos de trabalho de cada adulto.

## 7. Chat

- **R24.** Conversa individual com qualquer membro da família, como no WhatsApp.
- **R25.** **Qualquer membro** pode criar grupos. *(detalhe acrescentado em 05/10/2026, a revisar)* Quem cria dá o nome e **escolhe quem entra**, sempre entre membros da própria família. Um grupo só de crianças continua privado (R3).
- **R26.** Tempo real, com **histórico salvo**. Só entre membros da **mesma família**. *(detalhe acrescentado em 05/10/2026, a revisar)* A mensagem **chega na hora** para todos da conversa, **sem recarregar a página**, também nos grupos.

## 8. Agentes

- **R27.** *(texto atualizado em 02/10/2026, a revisar)* **Cada membro da família tem o seu agente pessoal.** Todos usam o **mesmo cérebro**; o que muda é o perfil de permissão de quem fala:
  - **Perfil adulto:** finanças, mercado, contas, hábitos, cenários, tarefas, estudo dos filhos e agenda.
  - **Perfil criança:** tarefas, horário de estudo, pontos, ranking, mural e pedido de troca. Nunca contas, rendas, ganhos ou gastos (R4). **Não ensina matéria escolar.**
- **R36.** *(texto proposto em 02/10/2026, a revisar)* **Privacidade dos agentes.** A conversa de cada membro com o seu agente é **privada**: nenhum outro agente e nenhuma outra pessoa a lê, nem os adultos. **Nenhum agente lê os chats** entre os membros (R24–R26). Fora isso, o agente enxerga todos os dados da família que o perfil de quem fala permite (R27, R28). Conversas nunca entram no treino do modelo base (R30).
- **R28.** O bloqueio de permissão fica **no código C#**, não na rede neural. A criança nunca recebe dado financeiro, mesmo que a rede entenda a pergunta.
- **R29.** **A rede entende → o código calcula → o agente explica.** Toda ação (lançar gasto, aceitar troca, registrar estudo) **pede confirmação**.
- **R30.** Todas as famílias começam com o mesmo **modelo base** (treinado em Python). O que cada família ensina ao agente **fica só com aquela família**.

---

## Pendências (decidir antes da implementação)

- [ ] **Estudo conta ponto?** Cumprir o horário de estudo do dia vale 1 ponto e entra na porcentagem do ranking (R19), ou fica fora da pontuação, só para conferência?
  - *Padrão provisório (05/10/2026):* ainda não conta; o estudo ainda não foi feito.
- [ ] **Quantos pontos custa pedir uma troca?** (R18)
  - *Padrão provisório no código:* **1 ponto** (`PedidoDeTroca.CustoEmPontos`), pago só se a troca for aceita.
- [ ] **Empate no TOP 1:** como desempatar? (R19)
  - *Padrão provisório no código:* ganha quem fez **mais tarefas**; se ainda empatar, os empatados **dividem o TOP 1** e todos vão para o mural.
- [ ] **Quem cria as tarefas?** (R16)
  - *Padrão provisório no código:* só os **adultos** criam e escolhem o responsável; cada um marca a própria tarefa, e um adulto pode marcar qualquer uma.

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



