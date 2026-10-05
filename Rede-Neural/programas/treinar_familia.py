"""Treina o classificador de intencao dos agentes da familia. Programa.

    python programas/gerar_corpus_familia.py
    python programas/treinar_familia.py

E O MESMO CLASSIFICADOR DA LOJA, COM OUTRO CORPUS

Nada em `rede/` mudou. Muda a pergunta: em vez de "quanto faturamos", e
"lancei 85 no mercado". A arquitetura (trigramas -> media -> oculta ->
softmax) serve igual, porque o problema tem a mesma forma.

A NOTA SO VALE COM AS DOBRAS AGRUPADAS POR BASE

Todas as variantes de uma frase-base caem na mesma dobra. A nota abaixo
mede se a rede entende um jeito de pedir que ela NUNCA viu, nem com outro
erro de digitacao.

O LIMIAR SAI DAS PREVISOES FORA DA DOBRA

Cada frase e prevista uma vez, pelo modelo que nao a viu no treino. Com
essas previsoes escolhe-se o menor limiar em que a rede acerta ao menos
`--precisao` das vezes que responde. Abaixo dele, o agente pergunta de
volta com botoes (ARCHITECTURE.md, 7.1).
"""

import argparse
import json
import sys
from collections import Counter
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from programas.treinar_intencao import dobras_por_base, treinar  # noqa: E402
from rede.texto import Vocabulario  # noqa: E402

RAIZ = Path(__file__).resolve().parents[1]
CORPUS = RAIZ / "familia" / "corpus.jsonl"
SAIDA = RAIZ / "familia" / "modelo_intencao.json"
SEMENTE = 42


def calibrar(confiancas, certos, precisao_alvo):
    """Menor limiar com precisao >= alvo. Devolve (limiar, precisao, cobertura)."""
    ordem = np.argsort(-confiancas)
    conf, ok = confiancas[ordem], certos[ordem]
    acumulado = np.cumsum(ok) / np.arange(1, len(ok) + 1)
    melhor = None
    for i in range(len(conf)):
        if acumulado[i] >= precisao_alvo:
            melhor = i
    if melhor is None:
        return 1.0, 0.0, 0.0
    return float(conf[melhor]), float(acumulado[melhor]), (melhor + 1) / len(conf)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--epocas", type=int, default=80)
    ap.add_argument("--taxa", type=float, default=1.0)
    # 96/48 venceu a varredura de 02/10/2026 (24/32: 44%, 64/32: 49%, 96/48: 54%).
    ap.add_argument("--dimensao", type=int, default=96)
    ap.add_argument("--ocultos", type=int, default=48)
    ap.add_argument("--dobras", type=int, default=5)
    ap.add_argument("--precisao", type=float, default=0.90)
    args = ap.parse_args()

    dados = [json.loads(l) for l in CORPUS.read_text(encoding="utf-8").splitlines() if l.strip()]
    perguntas = [d["pergunta"] for d in dados]
    rotulos = [d["intencao"] for d in dados]
    bases = [d["base"] for d in dados]
    intencoes = sorted(set(rotulos))
    n_int = {n: i for i, n in enumerate(intencoes)}

    print(f"corpus: {len(dados)} frases, {len(set(bases))} bases, {len(intencoes)} intencoes")
    print(f"CHUTAR A MAIOR    acerto {100*max(Counter(rotulos).values())/len(dados):5.1f}%")

    gerador = np.random.default_rng(SEMENTE)
    grupos = dobras_por_base(bases, rotulos, args.dobras, gerador)
    acertos = []
    conf_fora = np.zeros(len(dados))
    certo_fora = np.zeros(len(dados))
    escolha_fora = [""] * len(dados)
    nos_tres_fora = np.zeros(len(dados))

    for d in range(args.dobras):
        teste_i = sorted(set(grupos[d]))
        teste_set = set(teste_i)
        treino_i = [i for i in range(len(dados)) if i not in teste_set]
        voc = Vocabulario([perguntas[i] for i in treino_i])
        prep = lambda i: (voc.indices(perguntas[i]), n_int[rotulos[i]])
        c = treinar([prep(i) for i in treino_i], len(voc), intencoes,
                    args.epocas, args.taxa, args.dimensao, args.ocultos, SEMENTE + d)
        certos = 0
        for i in teste_i:
            indices = voc.indices(perguntas[i])
            intencao, conf = c.responder(indices)
            tres = np.argsort(-c.prever(indices).ravel())[:3]
            nos_tres_fora[i] = n_int[rotulos[i]] in tres
            conf_fora[i] = conf
            certo_fora[i] = intencao == rotulos[i]
            escolha_fora[i] = intencao
            certos += int(certo_fora[i])
        acertos.append(certos / len(teste_i))
        print(f"  dobra {d+1}   acerto {100*acertos[-1]:5.1f}%   ({len(teste_i)} frases)")

    media, desvio = 100 * np.mean(acertos), 100 * np.std(acertos)
    print(f"A REDE            acerto {media:5.1f}%  +- {desvio:.1f}"
          f"   (validacao cruzada agrupada, {args.dobras} dobras)")

    # Abaixo do limiar o agente mostra 3 botoes. Esta e a nota de "a certa
    # estava entre os botoes" — o que a pessoa experimenta de fato.
    tres = 100 * float(np.mean(nos_tres_fora))
    print(f"ENTRE OS 3 BOTOES acerto {tres:5.1f}%")

    limiar, prec, cob = calibrar(conf_fora, certo_fora, args.precisao)
    print(f"LIMIAR            {limiar:.2f}  -> responde {100*cob:.1f}% das frases"
          f" e acerta {100*prec:.1f}% delas; no resto pergunta com botoes")

    print("\nacerto por intencao (fora da dobra):")
    for nome in intencoes:
        idx = [i for i, r in enumerate(rotulos) if r == nome]
        a = np.mean([certo_fora[i] for i in idx])
        print(f"  {100*a:5.1f}%  {nome}")

    trocas = Counter((rotulos[i], escolha_fora[i]) for i in range(len(dados))
                     if not certo_fora[i])
    print("\nconfusoes mais comuns (verdade -> rede):")
    for (v, e), n in trocas.most_common(8):
        print(f"  {n:3d}  {v} -> {e}")

    # ---- modelo final: treinado em tudo ---------------------------------
    voc = Vocabulario(perguntas)
    prep = lambda i: (voc.indices(perguntas[i]), n_int[rotulos[i]])
    final = treinar([prep(i) for i in range(len(dados))], len(voc), intencoes,
                    args.epocas, args.taxa, args.dimensao, args.ocultos, SEMENTE)

    print("\nfrases que NAO estao no corpus:")
    for frase in ["gastei 37 no mercadinho", "quantos pontinhos eu tenho",
                  "ja lavei a louca", "comecei a licao de historia",
                  "como ta a classificacao", "qnt falta paga esse mes",
                  "me ajuda na prova de geografia", "e se eu comprar um carro"]:
        intencao, conf = final.responder(voc.indices(frase))
        marca = "" if conf >= limiar else "  <- pergunta com botoes"
        print(f"  {conf*100:5.1f}%  {intencao:24s} \"{frase}\"{marca}")

    modelo = final.para_dicionario(voc)
    modelo["limiar"] = round(limiar, 4)
    modelo["medido"] = {
        "epocas": args.epocas,
        "acerto_validacao_cruzada": round(float(media) / 100, 4),
        "acerto_entre_3_botoes": round(tres / 100, 4),
        "desvio": round(float(desvio) / 100, 4),
        "precisao_no_limiar": round(prec, 4),
        "cobertura_no_limiar": round(cob, 4),
        "corpus": len(dados),
        "bases": len(set(bases)),
    }
    SAIDA.write_text(json.dumps(modelo, separators=(",", ":")), encoding="utf-8")
    print(f"\ngravado: {SAIDA.relative_to(RAIZ)} ({SAIDA.stat().st_size/1024:.0f} KB,"
          f" {final.n_parametros} parametros)")


if __name__ == "__main__":
    main()
