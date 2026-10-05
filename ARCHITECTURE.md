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
