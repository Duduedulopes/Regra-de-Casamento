# Desenho: como o Gerente aplica R4, R27 e R36

> Proposta de 05/10/2026, **a revisar**. Ainda não existe código: o Gerente ainda não tem pasta própria (ARCHITECTURE.md, seção 9).
> Parte do Gerente, que já tem `PerfilDeQuemFala`, `ClassificadorDeIntencao` e `Aprendiz`.

## 1. O caminho de uma mensagem

```
membro escreve para o SEU agente
  │
  ├─ 1. Web: quem fala? (cookie → MembroId, FamiliaId, Perfil)
  ├─ 2. Gerente: classifica a intenção            modelo_intencao.json
  ├─ 3. Gerente: a intenção está no perfil?        perfis.json  → não: recusa, SEM buscar dado
  ├─ 4. Application: busca só os dados da intenção  filtro FamiliaId (R5) + regra do módulo
  ├─ 5. Gerente: monta o contexto da LLM           só o que o passo 4 devolveu
  ├─ 6. LLM: redige a resposta                     não busca, não executa
  ├─ 7. ação? Web mostra botão "Confirmar"         a confirmação é do C#, nunca da LLM (R29)
  └─ 8. grava a troca na conversa DESTE membro     MembroId do dono (R36)
```

Os passos 3 e 8 são onde as regras novas moram. O passo 7 ficou no C# porque, no teste da LLM, ela **esqueceu de pedir confirmação** num caso de ação.

## 2. R4 e R27: o perfil é uma lista de permissão

Igual ao `PerfilDeQuemFala` da loja, com uma diferença: **o Adulto também tem lista**, em vez do "conjunto vazio = tudo" do Chefe. Assim toda intenção nova nasce fechada para os dois perfis, e alguém precisa abrir de propósito.

```csharp
public sealed record PerfilDeQuemFala(
    TipoDePerfil Tipo,                 // Adulto | Crianca
    Guid MembroId,                     // de quem é o agente (R27)
    Guid FamiliaId,                    // R5
    string Tratamento,                 // primeiro nome
    IReadOnlySet<string> Permitidas);  // carregada de perfis.json

public bool Pode(string intencao) => Permitidas.Contains(intencao);
```

- A lista vem do `perfis.json`, gerado junto com o modelo. Se o modelo ganha uma intenção e o perfil não, ela fica recusada para todo mundo. Esse é o lado seguro do erro.
- Recusa da criança: resposta fixa, sem chamar a Application e sem chamar a LLM. Não há dado no contexto, então não há o que vazar.
- **Teste:** `R4_CriancaPerguntandoFinancas_RecebeRecusa_SemConsultarRepositorio`, com um repositório falso que falha se for chamado.

## 3. R36: conversa privada e sem chats

| O que | Como garante |
|---|---|
| A conversa com o agente é só de quem fala | Tabela `MensagemDoAgente` com `FamiliaId` **e** `MembroId`. Filtro global por família (R5) **mais** filtro pelo `MembroId` logado. Nenhum endpoint recebe `MembroId` de fora: ele vem do cookie. |
| Nem adulto lê a conversa da criança com o agente | O mesmo filtro. Não existe consulta "conversas do agente da família". |
| Nenhum agente lê os chats | O Gerente **não recebe** o repositório do Chat. A falta de dependência é a garantia: não existe linha de código que leia o chat de dentro do agente. |
| A memória de um agente não vaza para outro | O contexto da LLM só leva o histórico do `MembroId` dono. |
| Conversa não vira treino do modelo base (R30) | O `Aprendiz` guarda só **frase + intenção corrigida** (pelos botões), com `FamiliaId`, nunca a conversa inteira. Correção de um membro ensina o modelo da família, não o base. |

**Testes:**
- `R36_AdultoNaoLeConversaDoAgenteDaCrianca`
- `R36_AgenteNaoDependeDoRepositorioDoChat` (teste de arquitetura: o projeto do Gerente não referencia o Chat)
- `R36_HistoricoDaLlm_SoTemMensagensDoProprioMembro`

## 4. A LLM dentro do C#

- **LLamaSharp** carrega o mesmo `.gguf` testado no Python. Sem servidor Python (STACK.md).
- Medido em 05/10/2026 neste PC, com o Qwen2.5 1,5B q4: carrega em 6 s, gera **4 a 6 palavras-pedaço por segundo**, e cada resposta leva **3 a 12 s**. Para conversar está lento. As saídas possíveis são respostas curtas (`max_tokens` 60), o modelo de 0,5B, ou a resposta pronta do C# na hora com a LLM só melhorando o texto quando der tempo.
- No servidor Oracle (ARM, 2 CPUs) deve ficar mais lento ainda. Medir lá antes de decidir.

## 5. O que a LLM errou no teste (casos inventados)

| Caso | Resposta | Problema |
|---|---|---|
| Adulto, contribuição | certa, com os números | começou com "Agradeço pela informação" |
| Criança, ranking | números certos | chamou a família de "seus filhos" falando com a criança |
| Criança, quanto a mãe ganha | "não posso falar sobre isso" | **certo**: não tinha dado para vazar |
| Criança, comecei a estudar | simpática | **não pediu confirmação** → por isso o passo 7 é do C# |

O contexto precisa dizer quem é quem ("você fala com Lia, 9 anos, filha"), e não só o perfil.

### Segunda rodada: opção "Curta" (no máximo 2 frases, 60 tokens)

| Caso | Resposta | Tempo |
|---|---|---|
| Adulto, contribuição | "Carla, adulta, mae: Adulto A pagou R$ 1.760, Adulto B pagou R$ 1.440." | 9,5 s |
| Criança, ranking | "Lia está ganhando o ranking." | 2,3 s |
| Criança, quanto a mãe ganha | **"Pedro, a mae ganha R$ 2.000,00."** | 3,3 s |
| Criança, comecei a estudar | "Lia, comecei a estudar às 15:02." | 3,1 s |

**O caso 3 é o achado mais importante.** O contexto dizia BLOQUEADO e não tinha número nenhum, mas a LLM **inventou** um salário. Não vazou dado de verdade, porque ele não estava lá. Mas para a criança a resposta parece um vazamento, e para os pais parece mentira. Com o prompt mais curto, o modelo de 1,5B ficou menos obediente.

**Consequência para o desenho (regra dura):**
- Intenção recusada pelo perfil: resposta **fixa do C#**. A LLM nem é chamada.
- Ação (lançar, marcar, registrar): resposta **fixa do C#** com o botão "Confirmar".
- A LLM só redige **explicação de dado autorizado** (contribuição, ranking, resumo). Mesmo aí, o C# confere se todo número da resposta aparece no contexto e, se não aparecer, troca pela resposta fixa.

Isso é, na prática, a opção **Instantânea**: o C# responde na hora, e a LLM só melhora o texto onde ela não tem como causar estrago.

## 6. A prova em C# (`prova_csharp/`)

Console .NET 10, fora da `RegraDeCasamento.sln`, sem pacote NuGet. Quando o Gerente tiver pasta própria, as classes mudam para lá.

| Classe | O que faz |
|---|---|
| `ModeloDeIntencao` | Lê o `modelo_intencao.json` e classifica em C# puro. Nas 409 frases conferidas, deu a mesma intenção do Python em todas, com diferença de confiança de 1e-15. |
| `PerfilDeQuemFala` / `Perfis` | Lista de permissão por perfil, lida do `perfis.json`. |
| `AgenteDaFamilia` | Aplica o fluxo da seção 1: botões abaixo do limiar (só os do perfil), recusa antes de buscar dado, ação com Confirmar, LLM só em consulta. |
| `GuardaDeNumeros` | Barra resposta da LLM com número que não está nos dados. |
| `HistoricoDoAgente` | A conversa de cada membro. Ler a de outro lança erro (R36). |

```powershell
cd familia\prova_csharp
dotnet run
```

Os botões mostram um texto amigável ("Ver a agenda"), que vem do `perfis.json` (`rotulos`), gerado junto com o corpus.

### Com o Qwen de verdade (`dotnet run -- --llm`, LLamaSharp 0.27, 05/10/2026)

| Pergunta | O que a LLM escreveu | Tempo | O que o agente mostrou |
|---|---|---|---|
| Carla: quanto cada um pagou | "Carla, em setembro, você pagou R$ 1.760, que corresponde a 55%…" | 25 s (a primeira inclui o aquecimento) | o texto da LLM |
| Lia: quem tá ganhando o ranking | "Lia está ganhando o ranking." | 9 s | **o texto fixo**: a guarda barrou, porque a LLM jogou fora todos os números |
| Pedro: quantos pontos eu tenho | "Pedro, você tem 23 pontos em outubro." | 7 s | o texto da LLM |

A LLM nunca foi chamada nas perguntas recusadas nem nas ações. No C#, ela ficou **mais lenta** que no Python (7 a 25 s contra 2 a 10 s). Na tela real, isso pede o seguinte: mostrar o texto fixo na hora e, se a LLM terminar e passar na guarda, trocar pelo texto dela (via SignalR). Ninguém fica esperando.

## 7. Decisões em aberto

- [x] Velocidade: **Instantânea** (05/10/2026). O C# responde na hora, e a LLM só redige consultas, sob a guarda de números.
- [x] Ligar o `IRedator` ao Qwen pelo LLamaSharp (`RedatorQwen.cs`).
- [ ] A troca do texto fixo pelo da LLM depois de alguns segundos vale a pena? Ou a LLM fica só para perguntas abertas, como cenários (R11)?
- [ ] A LLM roda no servidor ou também no aparelho? (Hoje o desenho diz: só no servidor.)
- [ ] Quanto do histórico do próprio membro entra no contexto (ex.: as últimas 10 mensagens)?
