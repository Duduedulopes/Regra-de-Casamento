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
- [ ] **Quantos pontos custa pedir uma troca?** (R18)
- [ ] **Empate no TOP 1:** como desempatar? (R19)
