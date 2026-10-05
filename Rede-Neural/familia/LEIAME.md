# Cérebro dos agentes da família

> Cópia da Rede-Neural adaptada para a Regra de Casamento. A biblioteca `rede/` é a mesma da loja, sem nenhuma mudança. O que muda é o corpus.
> Começado em 02/10/2026.

## Como rodar

```powershell
python programas\gerar_corpus_familia.py   # gera corpus.jsonl e perfis.json
python programas\treinar_familia.py        # mede e grava modelo_intencao.json (~2,5 min)
```

## O que tem nesta pasta

| Arquivo | O que é |
|---|---|
| `corpus.jsonl` | 4.041 frases (619 bases escritas à mão e variantes com erro de digitação, "vc", "pra"). Cada frase cita a regra do BUSINESS_RULES.md que atende. |
| `perfis.json` | Quais intenções cada perfil pode pedir. O perfil **Adulto** tem as 29. O perfil **Crianca** tem 17: tarefas, estudo, agenda e conversa, e nunca finanças, mercado ou hábitos (R4). É o C# que confere isso (R28). |
| `modelo_intencao.json` | O classificador treinado, no mesmo formato JSON do Gerente, com o `limiar` e as medidas junto. |

## Números de 05/10/2026

Validação cruzada **agrupada por frase-base**: a nota mede jeitos de pedir que a rede nunca viu.

| | acerto |
|---|---|
| chutar a intenção mais comum | 5,6% |
| a rede, primeira resposta | **63,8%** ± 4,6 |
| a certa entre os 3 botões | **83,0%** |
| acima do limiar 0,97 | responde sozinha 35% das frases e acerta 90% delas |

Para comparar, a loja estava em 60,1% e 78,2% no caderno de 31/08.

| bases | acerto | entre 3 botões |
|---|---|---|
| 297 | 54% | — |
| 531 | 61,7% | 82,2% |
| 619 (frases de contraste) | 63,8% | 83,0% |

As frases de contraste melhoraram `comecar_estudo` (65→76%), `pedir_troca` (55→73%), `ver_resumo_financeiro` (28→55%) e `fora_do_escopo` (14→24%).

**O que ainda erra:** `fora_do_escopo` (24%, confunde com `ajuda`), `lancar_renda` × `lancar_despesa`, `pedir_troca` × `responder_troca`, `ver_mural` × `ver_ranking`, `cancelar` (47%). Frases curtas ("não", "s") são as mais difíceis para um modelo que tira a média das peças.

## Como cada agente funciona (R27, R28, R29, R36)

```
pergunta de um membro
  → classificador (este modelo)        entende a intenção
  → C#: perfil de quem fala            recusa se a intenção não está no perfil
  → C#: busca só os dados permitidos   nunca chats, nunca conversa de outro agente
  → LLM pequena                        só redige a resposta com esses dados
  → ação? pede confirmação
```

- Cada membro tem o **seu agente pessoal**. A conversa dele fica salva com o `MembroId` e só ele a vê.
- A LLM não busca dado nem executa nada. O que não está no contexto, ela não tem como contar.
- Conversa de ninguém entra no treino do modelo base (R30, R36).

## A LLM pequena

`programas/testar_llm.py` testa um modelo aberto já treinado como a "voz" do agente. O modelo é o Qwen2.5 1,5B q4, em `llm/` (~1 GB, fora do Git pelo `.gitignore`).

```powershell
python programas\testar_llm.py --modelo familia\llm\qwen2.5-1.5b-instruct-q4_k_m.gguf
```

Medido em 05/10/2026: carrega em 6 s e leva **3 a 12 s por resposta** (4 a 6 tokens/s). Os resultados e o desenho de como o Gerente em C# usa tudo isso estão em `GERENTE_DESENHO.md`.
