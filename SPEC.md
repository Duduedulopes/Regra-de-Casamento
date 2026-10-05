# SPEC.md — Regra de Casamento

> Feature atual. Só uma por vez: quando terminar, este arquivo é substituído pela próxima, e a anterior vai para o histórico no fim do arquivo.
> Última atualização: 05/10/2026

---

## Feature atual: **Mercado**

Cada compra detalhada (o que, quem comprou e para quê, R12), o histórico e a **planilha de controle** do casal (R13). Tudo **só para adultos** (R4). Cada base só começa quando a anterior **compila e passa nos testes**.

> Escrito pelo Claude em 05/10/2026, na sequência das Finanças. **A revisar.**
>
> **Situação em 05/10/2026:** as 4 bases foram feitas e testadas no navegador com o PostgreSQL 18; 0 erros, 0 avisos, 198 testes passando.
>
> **Ajuste nas Finanças feito antes (pedido do dono do projeto, "cada um precisa ter as suas próprias compras e dívidas"):**
> - **R37:** cada adulto lança e altera só o que é seu; o outro vê, mas não altera. O que é da casa, os dois. A tela mostra "Só fulano altera" nas contas do outro.
> - **R38:** dívidas (cartão, empréstimo, compra parcelada) com parcelas que viram contas a pagar no mês de cada uma, e o andamento (pagas, pago, falta). Aba **Dívidas** em `/financas` e `/api/financas/dividas`.

### Base 1 — As regras do mercado no Domain ✅
- `Compra`: dia, local, **quem comprou** (quem lança a compra, R37), **para quê** (suprimento da casa, lanche para todos, pessoal) e os **itens** (descrição, quantidade, valor unitário). Compra **pessoal** diz **para quem** (R12).
- `ResumoDoMercado`: total do mês, por finalidade e por quem comprou (R13).
- **Pronto quando:** testes de R12 e R13 com o código no nome.

### Base 2 — Banco e API ✅
- Tabelas `Compras` e `ItensDaCompra` (migration), com o filtro por família (R5).
- `/api/mercado`, só adultos: lançar compra, listar as compras do mês e o resumo.
- **Pronto quando:** testes de ponta a ponta, com criança recebendo 403 (R4) e família A sem ver as compras da família B (R5).

### Base 3 — Planilha de controle (R13) ✅
- `GET /api/mercado/planilha?ano=&mes=`: aba **Compras** (um item por linha) e aba **Resumo** (por finalidade e por quem comprou), com a mesma consulta da tela.
- **Pronto quando:** um teste abre a planilha e confere os valores.

### Base 4 — Tela ✅
- "Mercado": compras do mês, nova compra com vários itens, resumo e **Baixar planilha** (`/mercado`).

---

## Como a API responde

- Regra que não deixa: **422**, com o código da regra e a mensagem (`FalhaDeRegraDto`).
- Dado inválido (vazio, grande demais, valor ou quantidade zero): **400**, com a lista de erros (`ErrosDeDadosDto`).
- Sem login: **401**. Sem permissão: **403**.

## Decisões desta feature (padrões do Claude, a revisar)

- [ ] Compra **não vira despesa** nas Finanças automaticamente; as duas ficam separadas por enquanto.
- [ ] Quantidade aceita decimais (ex.: 1,5 kg). O total é quantidade × valor unitário.
- [ ] Compra lançada **não se apaga**, porque tudo fica no histórico (R13).
- [ ] "Pessoal" pode ser para qualquer membro, inclusive uma criança (ex.: lanche da escola); quem compra é sempre um adulto.

## Fora desta feature

Hábitos (R14, R15), cenários (R11), tarefas e ranking (R16 a R22), estudo, agenda, chat, agentes, PWA e publicação.

**Próximas, nesta ordem:** Tarefas e ranking (R16 a R22) → **Chat em tempo real, com grupos** (R24 a R26, R3, R36; SignalR) → os demais.

---

## Histórico de features

### Finanças da casa — 05/10/2026 (tela falta testar com o banco)

- **Base 1 — Domain:** `Despesa` (dono, a pagar/paga, quem pagou), `Renda` (tipo e dia de recebimento) e `ContribuicaoDoMes` (R6 a R10).
- **Base 2 — Banco e API:** tabelas `Despesas` e `Rendas` (migration `Financas`) e `/api/financas`, só para adultos.
- **Base 3 — Relatório (R9):** resumo da contribuição e planilha `.xlsx` (ClosedXML 0.105.1) com a mesma consulta.
- **Base 4 — Tela:** `/financas` com Contas, Rendas e Contribuição. **Falta** testar no navegador com o PostgreSQL ligado.
- **Decisões (padrões do Claude, a revisar):** reais com 2 casas; datas só com dia; contribuição = despesas da casa pagas no mês do pagamento; dia de recebimento de 1 a 31 com "dia útil"; renda da casa permitida.
- **Resultado:** 0 erros, 0 avisos, 158 testes passando.

### Família na prática — 02/10 a 05/10/2026 (Base 4 falta testar com o banco)

- **Base 1 — Conta e família nova:** cadastro de adulto (e-mail e senha), criar a família e ver a família (`/api/acesso/cadastrar`, `/api/familias`, `/api/familia`).
- **Base 2 — Entrar pelo código (R34):** pedir entrada, aprovar ou recusar, e atualizar o login sem sair.
- **Base 3 — Contas das crianças (R35):** adulto cria a criança com apelido e PIN; a criança entra com código, apelido e PIN.
- **Base 4 — Telas:** `/entrar`, `/cadastro`, `/comecar`, `/familia` com MudBlazor 9.11.0. **Falta** testar os fluxos no navegador com o PostgreSQL 18 ligado.
- **Decisões:** código da família, apelido + PIN e até 2 adultos (02/10/2026, pelas recomendações).
- **Em aberto:** limitar tentativas de pedir entrada com códigos errados.
- **Resultado:** 0 erros, 0 avisos, 110 testes passando.

### Fundação do projeto — concluída em 02/10/2026 (aguardando a revisão do dono do projeto)

> "Uma casa se constrói de base em base, para que fique tudo firme." — dono do projeto

- **Base 1 — A solução e os projetos:** `RegraDeCasamento.sln` com os projetos da ARCHITECTURE.md e as referências na direção dela. `dotnet build` sem erro e sem aviso; o app abre a página inicial.
- **Base 2 — O alicerce do Domain:** `Entidade` (Guid gerado no C#, datas em UTC), `IPertenceAFamilia` (R5) e `ResultadoDeDominio` (código da regra + mensagem, sem exceção).
- **Base 3 — As regras da família no Domain:** `Familia`, `Membro`, `PedidoDeEntrada`; R1, R5, R34 e R35 testadas, com o código da regra no nome do teste.
- **Base 4 — Banco e isolamento:** `DbContext` com PostgreSQL, migration `Inicial` e filtro global por `FamiliaId`. O PostgreSQL ainda não está instalado neste PC; o teste `R5_FamiliaANaoEnxergaNadaDaFamiliaB` roda em SQLite em memória.
- **Base 5 — Login e perfis:** Identity com cookie ligado ao `Membro`, perfis Adulto e Crianca, policy `Adulto`, migration `Acesso`. Teste `R4_CriancaLogada_RecebeAcessoNegadoNumEndpointDeAdulto`.
- **Resultado:** 0 erros, 0 avisos, 70 testes passando.
