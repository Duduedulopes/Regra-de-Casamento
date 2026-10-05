"""Testa uma LLM pequena, pronta, como a voz do agente. Programa.

    pip install llama-cpp-python
    python programas/testar_llm.py --modelo caminho/do/modelo.gguf

A LLM NAO DECIDE NADA

R29: a rede entende -> o codigo calcula -> o agente explica. Esta LLM e so o
"explica". Ela recebe tres coisas, montadas pelo C# no sistema de verdade:

    a intencao que a rede achou
    os dados que o PERFIL de quem fala pode ver (R4, R27, R28)
    a pergunta original

e escreve a resposta. Ela nunca busca dado, nunca executa acao, e nunca ve a
conversa de outra pessoa nem os chats (R36). O que nao esta no contexto, ela
nao tem como contar.

O QUE ESTE TESTE MEDE

Se um modelo de 0,5 a 1,5 bilhao de parametros, comprimido, roda neste PC
(sem placa de video) rapido o bastante para conversar, e se o portugues
dele e bom para a familia. Os casos abaixo sao inventados.
"""

import argparse
import time

CASOS = [
    {
        "quem": "Carla, adulta, mae",
        "perfil": "Adulto",
        "pergunta": "quanto cada um pagou esse mes",
        "intencao": "ver_contribuicao",
        "dados": "Contas da casa em setembro: R$ 3.200. Adulto A pagou R$ 1.760 (55%). "
                 "Adulto B pagou R$ 1.440 (45%).",
    },
    {
        "quem": "Lia, 9 anos, filha",
        "perfil": "Crianca",
        "pergunta": "quem ta ganhando o ranking",
        "intencao": "ver_ranking",
        "dados": "Ranking de outubro por porcentagem de tarefas cumpridas: Lia 92%, "
                 "Pedro 85%, Mae 80%, Pai 74%.",
    },
    {
        "quem": "Pedro, 11 anos, filho",
        "perfil": "Crianca",
        "pergunta": "quanto a mae ganha",
        "intencao": "ver_resumo_financeiro",
        "dados": "BLOQUEADO: o perfil Crianca nao tem acesso a financas (R4).",
    },
    {
        "quem": "Lia, 9 anos, filha",
        "perfil": "Crianca",
        "pergunta": "comecei a estudar",
        "intencao": "comecar_estudo",
        "dados": "Acao preparada: registrar inicio do estudo as 15:02. Planejado hoje: "
                 "15:00 as 16:00. Precisa de confirmacao.",
    },
]

# Opcao "Curta" (05/10/2026): no maximo 2 frases. A primeira medida, sem
# limite, deu 3 a 12 s por resposta neste PC.
SISTEMA = (
    "Voce e o agente pessoal de um membro de uma familia, no app Regra de Casamento. "
    "Responda em portugues do Brasil, gentil, em NO MAXIMO 2 frases curtas, sem saudacao. "
    "Fale direto com quem perguntou, chamando pelo nome. Use SOMENTE os dados fornecidos. "
    "Se os dados disserem BLOQUEADO, diga com carinho que voce nao pode falar disso. "
    "Nunca invente numeros. Com criancas, nao ensine materia escolar. "
    "Nao peca confirmacao: o app mostra o botao."
)
MAX_TOKENS = 60


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--modelo", required=True, help="arquivo .gguf")
    ap.add_argument("--threads", type=int, default=4)
    args = ap.parse_args()

    from llama_cpp import Llama

    inicio = time.perf_counter()
    llm = Llama(model_path=args.modelo, n_ctx=2048, n_threads=args.threads, verbose=False)
    print(f"carregou em {time.perf_counter() - inicio:.1f}s\n")

    for caso in CASOS:
        mensagens = [
            {"role": "system", "content": SISTEMA},
            {"role": "user", "content":
                f"Quem fala: {caso['quem']}\n"
                f"Perfil de quem fala: {caso['perfil']}\n"
                f"Intencao: {caso['intencao']}\n"
                f"Dados autorizados: {caso['dados']}\n"
                f"Pergunta: {caso['pergunta']}"},
        ]
        t = time.perf_counter()
        saida = llm.create_chat_completion(messages=mensagens, max_tokens=MAX_TOKENS, temperature=0.3)
        dt = time.perf_counter() - t
        texto = saida["choices"][0]["message"]["content"].strip()
        tokens = saida["usage"]["completion_tokens"]
        print(f"[{caso['perfil']}] {caso['pergunta']}")
        print(f"  {texto}")
        print(f"  ({dt:.1f}s, {tokens / dt:.1f} tokens/s)\n")


if __name__ == "__main__":
    main()
