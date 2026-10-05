"""Gera o corpus de intencoes dos agentes da familia. Programa.

    python programas/gerar_corpus_familia.py

DE ONDE VEM CADA INTENCAO

Cada intencao aponta para a regra do BUSINESS_RULES.md da Regra de Casamento
que ela atende (R6, R12, R16...). Intencao sem regra nao entra: o agente so
entende o que o sistema sabe fazer.

FRASES-BASE ESCRITAS A MAO, VARIANTES GERADAS

As frases-base sao o que importa. As variantes (erro de digitacao, "vc",
"pra", sem artigo) so ensinam a aguentar gente escrevendo rapido. Toda
variante guarda a `base` de onde saiu, para que a validacao cruzada ponha
base e variantes na MESMA dobra. Sem isso a nota mede se a rede desfaz o
erro que este script escreveu, e nao se ela entende um jeito novo de pedir.

OS VALORES MUDAM A CADA VARIANTE

"lancei 85 no mercado" e "lancei 120 no mercado" sao a mesma intencao. Se o
numero fosse sempre o mesmo, a rede aprenderia que "85" quer dizer compra.
"""

import json
import random
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

RAIZ = Path(__file__).resolve().parents[1]
SAIDA = RAIZ / "familia" / "corpus.jsonl"
SEMENTE = 42
VARIANTES_POR_BASE = 6

# ---------------------------------------------------------------------------
# As intencoes, o modulo e a regra de cada uma.
# ---------------------------------------------------------------------------

INTENCOES = {
    # ---- Financas (R6-R11) -------------------------------------------------
    "lancar_despesa": ("financas", "R6", [
        "lanca uma conta de {v} reais",
        "paguei a luz hoje, {v}",
        "registra a conta de agua de {v}",
        "coloca o aluguel de {v} nas contas da casa",
        "anota que eu paguei {v} de internet",
        "tem uma despesa nova de {v}",
        "paguei {v} no gas",
        "adiciona a fatura do cartao de {v}",
        "registra o boleto da escola, {v}",
        "lancei a parcela do carro de {v}",
        "nova conta pra pagar de {v}",
        "gastei {v} com o conserto da maquina",
    ]),
    "lancar_renda": ("financas", "R7", [
        "recebi meu salario de {v}",
        "caiu {v} de comissao",
        "lanca uma renda de {v}",
        "entrou a pensao, {v}",
        "registra o vale alimentacao de {v}",
        "recebi {v} de um freela",
        "anota que entrou dinheiro, {v}",
        "meu pagamento caiu hoje",
        "ganhei {v} esse mes",
        "registra o que eu recebi, {v}",
        "entrou um extra de {v}",
    ]),
    "ver_contas_a_pagar": ("financas", "R10", [
        "quais contas faltam pagar",
        "o que tem pra pagar esse mes",
        "tem conta vencendo",
        "mostra as contas em aberto",
        "quais boletos ainda nao pagamos",
        "o que vence essa semana",
        "lista as contas a pagar",
        "falta pagar alguma coisa",
        "tem alguma conta atrasada",
        "quanto ainda temos pra pagar",
    ]),
    "marcar_conta_paga": ("financas", "R10", [
        "marca a conta de luz como paga",
        "ja paguei o aluguel",
        "a internet ta paga",
        "pode dar baixa na conta de agua",
        "o boleto da escola ja foi pago",
        "marca como pago o cartao",
        "quitei a parcela do carro",
        "a conta do gas ja foi",
        "coloca como paga a fatura",
        "paguei aquela conta que tava em aberto",
    ]),
    "ver_contribuicao": ("financas", "R9", [
        "quanto cada um pagou esse mes",
        "qual a minha parte nas contas da casa",
        "quem pagou mais esse mes",
        "mostra a contribuicao de cada um",
        "quanto eu paguei das contas",
        "quanto ela pagou esse mes",
        "como ficou a divisao das contas",
        "qual a porcentagem que cada um pagou",
        "quanto ele contribuiu esse mes",
        "historico de quem pagou o que",
    ]),
    "simular_cenario": ("financas", "R11", [
        "e se eu alugar um carro pra fazer uber",
        "simula se a gente mudar de casa",
        "da pra trocar de carro esse ano",
        "e se eu ganhar {v} a mais",
        "se o aluguel subir pra {v} como fica",
        "simula uma viagem de {v}",
        "vale a pena financiar uma moto",
        "e se a gente cortar a internet",
        "faz uma previsao se eu sair do emprego",
        "como ficaria se a gente economizar {v} por mes",
    ]),
    "ver_resumo_financeiro": ("financas", "R6", [
        "como estao as financas",
        "quanto sobrou esse mes",
        "qual o saldo da casa",
        "quanto entrou e quanto saiu",
        "resumo do dinheiro do mes",
        "quanto a gente gastou esse mes",
        "como ta o orcamento",
        "estamos no vermelho",
        "quanto temos de renda no total",
        "balanco do mes",
    ]),

    # ---- Mercado (R12, R13) ------------------------------------------------
    "lancar_compra": ("mercado", "R12", [
        "lancei {v} no mercado",
        "fiz compra no mercado de {v}",
        "comprei {item} pra casa",
        "gastei {v} no mercado",
        "registra uma compra de {item}",
        "pedi um lanche pra todo mundo de {v}",
        "comprei {item} pra mim",
        "anota a compra do mercado, {v}",
        "fui no atacadao e gastei {v}",
        "compra de {item} pra casa, {v}",
        "pedimos pizza, {v}",
        "comprei {item} de {v}",
    ]),
    "ver_gastos_mercado": ("mercado", "R13", [
        "quanto gastamos de mercado esse mes",
        "mostra o historico do mercado",
        "o que compramos semana passada",
        "quanto foi de lanche esse mes",
        "quanto custou o mercado do mes",
        "lista as compras do mes",
        "gera a planilha do mercado",
        "quanto eu gastei comigo esse mes",
        "quem comprou mais no mercado",
        "quanto gastamos com {item}",
    ]),

    # ---- Habitos (R14, R15) ------------------------------------------------
    "registrar_habito": ("habitos", "R14", [
        "fumei {n} cigarros hoje",
        "tomei um energetico",
        "comprei um maco de cigarro",
        "anota {n} energeticos hoje",
        "fui na academia hoje",
        "registra um cigarro",
        "tomei {n} latas de energetico",
        "treinei hoje",
        "acabei de fumar",
        "marca que eu fui pra academia",
    ]),
    "ver_habitos": ("habitos", "R14", [
        "quantos cigarros eu fumei essa semana",
        "quanto gastei com energetico",
        "quanto custa meu cigarro por mes",
        "quantas vezes fui na academia",
        "mostra meus habitos",
        "to fumando menos",
        "quanto foi de energetico esse mes",
        "relatorio dos habitos",
        "quanto a gente gasta com academia",
        "compara meu cigarro com o mes passado",
    ]),

    # ---- Tarefas, pontos e reconhecimento (R16-R22) -------------------------
    "ver_minhas_tarefas": ("tarefas", "R16", [
        "quais sao minhas tarefas hoje",
        "o que eu tenho que fazer",
        "mostra meu checklist",
        "falta alguma tarefa",
        "o que ainda nao fiz hoje",
        "qual minha tarefa de hoje",
        "tenho tarefa pra fazer",
        "lista do que eu preciso fazer",
        "o que falta eu fazer em casa",
        "minhas obrigacoes de hoje",
    ]),
    "marcar_tarefa_feita": ("tarefas", "R17", [
        "terminei de {tarefa}",
        "ja fiz {tarefa}",
        "marca {tarefa} como feito",
        "pronto, {tarefa} ta feito",
        "acabei {tarefa}",
        "fiz minha tarefa",
        "conclui {tarefa}",
        "pode marcar que eu fiz {tarefa}",
        "tarefa feita",
        "ja terminei a minha parte",
    ]),
    "pedir_troca": ("tarefas", "R18", [
        "quero trocar minha tarefa",
        "troca {tarefa} comigo",
        "posso trocar de funcao com alguem",
        "pede pra alguem fazer {tarefa} no meu lugar",
        "quero passar {tarefa} pra outra pessoa",
        "alguem troca comigo hoje",
        "pago pontos pra trocar {tarefa}",
        "nao quero {tarefa}, troca pra mim",
        "faz um pedido de troca",
        "troca minha tarefa de hoje",
    ]),
    "responder_troca": ("tarefas", "R18", [
        "aceito a troca",
        "topo trocar",
        "nao quero trocar",
        "recuso a troca",
        "pode trocar sim",
        "aceita o pedido de troca",
        "nao aceito trocar com ele",
        "beleza, eu faco a tarefa dela",
        "tem algum pedido de troca pra mim",
        "responde que eu aceito a troca",
    ]),
    "ver_pontos": ("tarefas", "R17", [
        "quantos pontos eu tenho",
        "qual minha pontuacao",
        "meus pontos",
        "quantos pontos eu fiz esse mes",
        "ja tenho quantos pontos",
        "me fala meus pontos",
        "quantos pontos ela tem",
        "quanto eu pontuei hoje",
        "ganhei ponto",
        "saldo de pontos",
    ]),
    "ver_ranking": ("tarefas", "R19", [
        "quem ta ganhando o ranking",
        "mostra o ranking",
        "quem ta em primeiro",
        "quem vai ser o top 1 do mes",
        "em que lugar eu to",
        "quem e o membro do mes",
        "como ta a disputa esse mes",
        "to na frente de quem",
        "classificacao da familia",
        "quem ta liderando",
    ]),
    "ver_mural": ("tarefas", "R20", [
        "mostra o mural",
        "quem foi o top 1 do mes passado",
        "quem ganhou nos outros meses",
        "abre o mural da familia",
        "historico dos campeoes",
        "quantas vezes eu fui top 1",
        "quem escolhe o lanche de sabado",
        "quem ganhou o lanche esse mes",
        "mural dos vencedores",
        "quem foi o membro do mes em agosto",
    ]),

    # ---- Estudo (R31-R33) --------------------------------------------------
    "comecar_estudo": ("estudo", "R32", [
        "comecei a estudar",
        "vou estudar agora",
        "to estudando",
        "inicia meu estudo",
        "comecando a licao agora",
        "abri o caderno, comecei",
        "marca que comecei a estudar",
        "hora de estudar, comecei",
        "to fazendo o dever agora",
        "comeca a contar meu estudo",
    ]),
    "terminar_estudo": ("estudo", "R32", [
        "terminei de estudar",
        "acabei o estudo",
        "parei de estudar",
        "terminei a licao",
        "pronto, estudei",
        "finaliza meu estudo",
        "acabei o dever de casa",
        "marca que terminei de estudar",
        "chega de estudar por hoje",
        "fim do estudo",
    ]),
    "ver_horario_estudo": ("estudo", "R31", [
        "qual meu horario de estudo",
        "que horas eu tenho que estudar",
        "hoje tem estudo",
        "quanto tempo eu tenho que estudar hoje",
        "quando e meu proximo estudo",
        "mostra meu horario de estudo",
        "eu estudo amanha",
        "quanto eu ja estudei hoje",
        "falta quanto tempo de estudo",
        "meu cronograma de estudo",
    ]),
    "conferir_estudo_filhos": ("estudo", "R33", [
        "as criancas estudaram hoje",
        "ele cumpriu o horario de estudo",
        "confere o estudo dos meninos",
        "quanto ela estudou essa semana",
        "o estudo bateu com o planejado",
        "meu filho estudou hoje",
        "compara o estudo planejado com o realizado",
        "quem nao estudou hoje",
        "relatorio de estudo das criancas",
        "ela fez a licao",
    ]),

    # ---- Agenda (R23) ------------------------------------------------------
    "ver_agenda": ("agenda", "R23", [
        "o que temos na agenda hoje",
        "qual o compromisso de amanha",
        "mostra a agenda da familia",
        "quando e o tempo do casal",
        "que horas eu trabalho amanha",
        "tem alguma coisa marcada no sabado",
        "minha agenda da semana",
        "que horas e a academia",
        "qual o turno dela essa semana",
        "o que a familia vai fazer no domingo",
    ]),

    # ---- Conversa (sem modulo) ---------------------------------------------
    "saudacao": ("conversa", "R29", [
        "oi",
        "bom dia",
        "boa noite",
        "ola agente",
        "e ai",
        "oi tudo bem",
        "boa tarde",
        "fala ai",
        "opa",
        "oi, to aqui",
    ]),
    "ajuda": ("conversa", "R29", [
        "o que voce sabe fazer",
        "me ajuda",
        "como voce funciona",
        "quais comandos eu posso usar",
        "pra que serve voce",
        "o que posso te pedir",
        "nao sei usar isso",
        "me explica o que voce faz",
        "ajuda por favor",
        "como eu uso o agente",
    ]),
    "confirmar": ("conversa", "R29", [
        "sim",
        "pode",
        "confirmo",
        "isso mesmo",
        "pode fazer",
        "ok, manda",
        "isso",
        "certo",
        "pode sim",
        "beleza, confirma",
    ]),
    "cancelar": ("conversa", "R29", [
        "nao",
        "cancela",
        "deixa pra la",
        "esquece",
        "nao faz isso",
        "para",
        "desisti",
        "melhor nao",
        "nao, ta errado",
        "volta, cancela isso",
    ]),
    "agradecer": ("conversa", "R29", [
        "obrigado",
        "valeu",
        "brigado",
        "muito obrigada",
        "valeu agente",
        "show, obrigado",
        "agradeco",
        "obrigado pela ajuda",
        "tmj",
        "valeu mesmo",
    ]),
    "fora_do_escopo": ("conversa", "R27", [
        "me ajuda com a licao de matematica",
        "quanto e 7 vezes 8",
        "me explica fotossintese",
        "faz minha redacao",
        "qual a capital da franca",
        "conta uma piada",
        "vai chover amanha",
        "qual o resultado do jogo",
        "me ensina ingles",
        "resolve essa conta de fracao",
        "quem descobriu o brasil",
        "traduz essa frase pra mim",
    ]),
}

# Mais jeitos de pedir. Com dez bases por intencao a rede acertava 54% das
# formas que nunca viu; o que mais sobe essa nota e base nova, nao ajuste.
EXTRAS = {
    "lancar_despesa": ["chegou a conta de luz, {v}", "bota ai uma despesa de {v}",
                       "o condominio veio {v}", "paguei o medico, deu {v}",
                       "cadastra uma conta nova", "a conta do celular foi {v}",
                       "registra um gasto da casa de {v}", "tive que pagar {v} de multa"],
    "lancar_renda": ["meu salario entrou", "ela recebeu {v} de bonus",
                     "cadastra uma entrada de {v}", "vendi uma coisa por {v}",
                     "recebi o decimo terceiro", "caiu o beneficio, {v}",
                     "o freela pagou {v}", "lanca o que entrou hoje"],
    "ver_contas_a_pagar": ["o que ta em aberto", "quais contas vencem amanha",
                           "tem boleto pra pagar", "o que ainda falta quitar",
                           "me mostra o que precisa pagar", "quais as proximas contas",
                           "alguma conta pendente", "contas do mes que faltam"],
    "marcar_conta_paga": ["essa conta ja foi paga", "paguei o condominio agora",
                          "pode tirar a luz das pendentes", "da como pago o boleto",
                          "a conta do celular ta quitada", "ja acertei o aluguel",
                          "pagamento do gas feito", "fechei a fatura do cartao"],
    "ver_contribuicao": ["quanto eu contribui pra casa", "quem pagou as contas da casa",
                         "minha participacao nas contas", "quanto cada um colocou em casa",
                         "divisao do mes passado", "quanto ela colocou nas contas",
                         "parte de cada um nas despesas", "quanto eu banquei esse mes"],
    "simular_cenario": ["e se eu trocar de emprego", "da pra gente comprar um sofa de {v}",
                        "simula um emprestimo de {v}", "se eu fizer uber de noite compensa",
                        "consigo juntar {v} ate dezembro", "e se a gente tiver mais um filho",
                        "como fica se a luz aumentar", "projeta os gastos do ano que vem"],
    "ver_resumo_financeiro": ["fechou o mes no azul", "sobrou dinheiro",
                              "quanto gastamos no total", "como ta nossa situacao financeira",
                              "entrada e saida do mes", "relatorio financeiro",
                              "a gente ta gastando muito", "quanto falta pro fim do mes"],
    "lancar_compra": ["passei no mercado e deu {v}", "compra do mes deu {v}",
                      "pedi um acai pra todos", "comprei {item} e {item}",
                      "registra o mercado de hoje", "gastei {v} na padaria",
                      "compra pessoal de {v}", "feira de hoje deu {v}"],
    "ver_gastos_mercado": ["quanto foi de padaria", "historico das compras",
                           "o que mais compramos", "gasto com lanche do mes",
                           "planilha de compras", "quanto vai de mercado por semana",
                           "compras pessoais do mes", "quanto gastei no mercado"],
    "registrar_habito": ["fumei agora", "mais um energetico", "fui treinar",
                         "registra academia hoje", "tomei um monster",
                         "fumei meio maco", "anota um cigarro pra mim", "malhei hoje cedo"],
    "ver_habitos": ["quanto eu fumo por dia", "gasto com cigarro",
                    "quantos energeticos essa semana", "frequencia na academia",
                    "meus vicios do mes", "to tomando muito energetico",
                    "quanto gastei com cigarro", "resumo dos meus habitos"],
    "ver_minhas_tarefas": ["o que eu tenho pra fazer hoje", "minhas tarefas",
                           "qual e a minha funcao hoje", "o que me falta",
                           "tarefas pendentes", "o que sobrou pra mim",
                           "checklist de hoje", "qual minha parte na casa hoje"],
    "marcar_tarefa_feita": ["ja {tarefa}", "feito, {tarefa}", "a louca ta lavada",
                            "o quarto ta arrumado", "lixo ja foi", "ta feito",
                            "cumpri minha tarefa", "terminei o que tinha que fazer"],
    "pedir_troca": ["troca comigo", "alguem faz {tarefa} pra mim", "quero mudar de tarefa",
                    "dou pontos pra quem fizer {tarefa}", "nao vou poder {tarefa}, troca",
                    "pede troca com a minha irma", "quero trocar com o pai",
                    "tem como trocar a tarefa"],
    "responder_troca": ["aceito", "eu troco", "fechado, troco com voce",
                        "nao vou aceitar a troca", "pode aceitar a troca dele",
                        "rejeita a troca", "quem pediu troca comigo", "topo sim a troca"],
    "ver_pontos": ["meus pontos do mes", "pontuacao", "quanto eu tenho de ponto",
                   "quantos pontos ganhei", "ver pontos", "pontos acumulados",
                   "quantos pontos faltam", "quanto ponto o irmao tem"],
    "ver_ranking": ["ranking", "posicao no ranking", "quem ta ganhando",
                    "quem ta na frente", "qual minha colocacao", "placar da familia",
                    "quem ta em ultimo", "quem tem a maior porcentagem"],
    "ver_mural": ["mural", "campeoes anteriores", "quem venceu ano passado",
                  "galeria dos top 1", "quem ja ganhou", "os vencedores",
                  "lista dos top 1", "quem foi o melhor do mes passado"],
    "comecar_estudo": ["vou comecar a estudar", "iniciando o estudo",
                       "liga o estudo", "comecei a licao", "sentei pra estudar",
                       "agora vou fazer a tarefa da escola", "comecei", "inicio do estudo"],
    "terminar_estudo": ["acabei", "terminei o dever", "encerra o estudo",
                        "terminei a licao de casa", "desliga o estudo",
                        "fim da licao", "parei agora", "acabei de estudar"],
    "ver_horario_estudo": ["horario de estudo", "que dia eu estudo",
                           "tenho estudo hoje", "quando eu estudo",
                           "que horas e o estudo", "meu horario da licao",
                           "quantas horas de estudo hoje", "agenda de estudo"],
    "conferir_estudo_filhos": ["os filhos estudaram", "o menino fez o dever",
                               "estudo das criancas hoje", "quanto tempo ele estudou",
                               "ela cumpriu o estudo", "as criancas fizeram a licao",
                               "conferir o estudo", "planejado e realizado das criancas"],
    "ver_agenda": ["agenda", "o que tem amanha", "compromissos da semana",
                   "quando a gente sai junto", "qual o horario de trabalho dela",
                   "o que tem marcado", "programacao do fim de semana",
                   "que dia e o tempo da familia"],
    "saudacao": ["ola", "oie", "bom diaa", "fala", "salve", "oii", "eae", "boa"],
    "ajuda": ["o que vc faz", "ajuda", "socorro, nao entendi", "como funciona",
              "me mostra as opcoes", "o que da pra fazer aqui", "tutorial", "comandos"],
    "confirmar": ["s", "sim pode", "manda ver", "confirmado", "ta certo", "positivo",
                  "fechado", "pode ser"],
    "cancelar": ["n", "nao quero", "cancelar", "anula", "para tudo", "negativo",
                 "nem pensar", "errado, cancela"],
    "agradecer": ["obg", "vlw", "obrigadao", "valeu pela forca", "gratidao",
                  "agradecido", "obrigada agente", "muito bom, valeu"],
    "fora_do_escopo": ["quanto e 15 mais 27", "me ajuda no trabalho de historia",
                       "o que e um substantivo", "escreve um poema", "quem ganhou a copa",
                       "me ajuda na prova de ciencias", "qual a raiz de 81",
                       "como se fala cachorro em ingles", "me conta uma historia",
                       "qual o melhor filme"],
}

# Rodada 3: os pares que mais se confundiam em 02/10/2026. Cada frase aqui
# carrega a palavra que separa um lado do outro ("quero trocar" x "aceito";
# "comecei" x "terminei"; "me ensina" x "o que voce faz").
CONTRASTES = {
    "pedir_troca": ["eu quero trocar a louca pelo lixo", "manda um pedido de troca pra ela",
                    "quero que alguem fique com a minha tarefa", "posso pagar {n} pontos pra trocar",
                    "abre uma troca da minha tarefa", "to pedindo pra trocar hoje",
                    "pede pra minha mae trocar comigo", "quero ceder minha tarefa",
                    "me tira dessa tarefa e passa pra outro", "solicita troca de tarefa"],
    "responder_troca": ["aceitei o pedido dela", "sim, aceito trocar com ele",
                        "ele me pediu troca e eu aceito", "nao aceito o pedido de troca",
                        "recusa o pedido que me mandaram", "fico com a tarefa dele sim",
                        "pedido de troca aceito", "tem troca esperando minha resposta",
                        "quero ver os pedidos de troca que recebi", "concordo com a troca"],
    "comecar_estudo": ["to comecando a estudar agora", "vou abrir o livro e comecar",
                       "inicia o cronometro do estudo", "comecei a estudar matematica",
                       "ja to estudando desde agora", "bora estudar, comecei",
                       "pode marcar o inicio do meu estudo", "comecando a licao de casa"],
    "terminar_estudo": ["terminei de estudar matematica", "acabei a licao agora",
                        "para o cronometro do estudo", "pode marcar o fim do meu estudo",
                        "ja estudei, terminei", "fechei o livro, acabou",
                        "terminei meu horario de estudo", "encerrei a licao"],
    "fora_do_escopo": ["me ensina a fazer conta de dividir", "faz o dever de casa pra mim",
                       "qual a formula da area do triangulo", "explica o que e verbo",
                       "quanto e 12 dividido por 4", "me passa a resposta da prova",
                       "o que e celula", "me ajuda a estudar pra prova",
                       "como se escreve exceção", "qual o maior planeta",
                       "canta uma musica", "quem e o presidente",
                       "me fala sobre dinossauros", "como faz bolo de chocolate"],
    "ajuda": ["o que voce consegue fazer por mim", "quais coisas posso te pedir",
              "me explica como usar o app", "nao sei o que pedir",
              "lista tudo que voce faz", "como eu falo com voce"],
    "ver_resumo_financeiro": ["como ficou o dinheiro da casa esse mes", "saldo do mes",
                              "quanto a familia ganhou e gastou", "fechamento do mes",
                              "o dinheiro vai dar ate o fim do mes", "resultado financeiro do mes",
                              "quanto sobra depois das contas", "visao geral das financas"],
    "ver_gastos_mercado": ["quanto foi so de mercado", "gastos de supermercado do mes",
                           "historico do supermercado", "o que eu comprei no mercado ontem"],
    "confirmar": ["sim, pode fazer", "confirma ai", "isso, pode lancar", "ta bom, pode",
                  "pode confirmar", "certinho"],
    "cancelar": ["nao, cancela isso", "nao pode", "nao era isso", "cancela por favor",
                 "deixa quieto", "nao confirma"],
    "ver_ranking": ["quem ta em segundo", "minha posicao esse mes", "ranking de agora",
                    "porcentagem de cada um no mes"],
    "ver_mural": ["quem foi top 1 em setembro", "mural dos meses passados",
                  "historico do top 1", "quem ganhou o mural antes"],
}
for _fonte in (EXTRAS, CONTRASTES):
    for _nome, _frases in _fonte.items():
        INTENCOES[_nome][2].extend(_frases)

# O texto do botao quando a rede nao tem certeza. Viaja no perfis.json junto
# com a lista de permissao: intencao nova sem rotulo aparece com o nome tecnico,
# e o teste do C# avisa.
ROTULOS = {
    "lancar_despesa": "Lançar uma conta",
    "lancar_renda": "Lançar um dinheiro que entrou",
    "ver_contas_a_pagar": "Ver as contas a pagar",
    "marcar_conta_paga": "Marcar uma conta como paga",
    "ver_contribuicao": "Ver quanto cada um pagou",
    "simular_cenario": "Simular um cenário",
    "ver_resumo_financeiro": "Ver o resumo do mês",
    "lancar_compra": "Lançar uma compra",
    "ver_gastos_mercado": "Ver os gastos do mercado",
    "registrar_habito": "Registrar um hábito",
    "ver_habitos": "Ver meus hábitos",
    "ver_minhas_tarefas": "Ver minhas tarefas",
    "marcar_tarefa_feita": "Marcar tarefa como feita",
    "pedir_troca": "Pedir uma troca de tarefa",
    "responder_troca": "Responder um pedido de troca",
    "ver_pontos": "Ver meus pontos",
    "ver_ranking": "Ver o ranking",
    "ver_mural": "Ver o mural",
    "comecar_estudo": "Começar o estudo",
    "terminar_estudo": "Terminar o estudo",
    "ver_horario_estudo": "Ver meu horário de estudo",
    "conferir_estudo_filhos": "Conferir o estudo das crianças",
    "ver_agenda": "Ver a agenda",
    "saudacao": "Só dizer oi",
    "ajuda": "Ver o que você faz",
    "confirmar": "Confirmar",
    "cancelar": "Cancelar",
    "agradecer": "Agradecer",
    "fora_do_escopo": "Outra coisa",
}
assert set(ROTULOS) == set(INTENCOES), "toda intencao precisa de rotulo"

# Quem pode pedir o que (R27, R28). O C# confere isto; a rede so entende.
# Criança: nunca contas, rendas, ganhos ou gastos (R4).
PERFIS = {
    "Adulto": sorted(INTENCOES),
    "Crianca": sorted(i for i, (modulo, _, _) in INTENCOES.items()
                      if modulo in {"tarefas", "estudo", "agenda", "conversa"}
                      and i != "conferir_estudo_filhos"),
}

# ---------------------------------------------------------------------------
# Os valores das lacunas.
# ---------------------------------------------------------------------------

VALORES = ["85", "120", "42,90", "300", "1500", "79", "250", "18", "65,50", "2000"]
NUMEROS = ["2", "3", "5", "1", "10", "uns 4"]
ITENS = ["arroz", "feijao", "carne", "fralda", "papel higienico", "leite",
         "sabao em po", "frutas", "pao", "refrigerante", "shampoo", "verdura"]
TAREFAS = ["lavar a louca", "arrumar o quarto", "tirar o lixo", "varrer a casa",
           "dar comida pro cachorro", "estender a roupa", "limpar o banheiro",
           "guardar os brinquedos"]


def preencher(frase, r):
    return (frase.replace("{v}", r.choice(VALORES))
                 .replace("{n}", r.choice(NUMEROS))
                 .replace("{item}", r.choice(ITENS))
                 .replace("{tarefa}", r.choice(TAREFAS)))


# ---------------------------------------------------------------------------
# As variantes: o jeito de escrever de quem digita no celular.
# ---------------------------------------------------------------------------

INFORMAL = {
    "voce": "vc", "você": "vc", "para": "pra", "estou": "to", "esta": "ta",
    "que": "q", "hoje": "hj", "quanto": "qnt", "quantos": "qnts",
    "porque": "pq", "tambem": "tb", "comigo": "cmg", "nao": "n",
    "beleza": "blz", "obrigado": "obg", "mesmo": "msm", "tudo": "td",
    "aqui": "aki", "mensagem": "msg", "pagar": "paga", "agora": "agr",
}
ARTIGOS = {"o", "a", "os", "as", "um", "uma", "do", "da", "no", "na", "de"}
PREFIXOS = ["", "", "", "ei, ", "agente, ", "oi, ", "por favor ", "rapidinho, "]
SUFIXOS = ["", "", "", " por favor", " ai", " pfv", "?", " agora"]


def erro_de_digitacao(palavra, r):
    if len(palavra) < 4:
        return palavra
    i = r.randrange(1, len(palavra) - 1)
    tipo = r.choice(["some", "troca", "dobra"])
    if tipo == "some":
        return palavra[:i] + palavra[i + 1:]
    if tipo == "troca":
        return palavra[:i - 1] + palavra[i] + palavra[i - 1] + palavra[i + 1:]
    return palavra[:i] + palavra[i] + palavra[i:]


def variante(frase, r):
    palavras = frase.split()
    if r.random() < 0.6:
        palavras = [INFORMAL.get(p, p) if r.random() < 0.7 else p for p in palavras]
    if r.random() < 0.4 and len(palavras) > 3:
        palavras = [p for p in palavras if p not in ARTIGOS] or palavras
    if r.random() < 0.5:
        j = r.randrange(len(palavras))
        palavras[j] = erro_de_digitacao(palavras[j], r)
    texto = " ".join(palavras)
    return r.choice(PREFIXOS) + texto + r.choice(SUFIXOS)


def main():
    r = random.Random(SEMENTE)
    linhas = []
    for intencao, (modulo, regra, bases) in INTENCOES.items():
        for base in bases:
            vistas = set()
            for k in range(VARIANTES_POR_BASE + 1):
                molde = base if k == 0 else variante(base, r)
                frase = preencher(molde, r)
                if frase in vistas:
                    continue
                vistas.add(frase)
                linhas.append({"pergunta": frase, "intencao": intencao,
                               "modulo": modulo, "regra": regra, "base": base,
                               "origem": "base" if k == 0 else "variante"})

    SAIDA.parent.mkdir(exist_ok=True)
    with open(SAIDA, "w", encoding="utf-8") as f:
        for l in linhas:
            f.write(json.dumps(l, ensure_ascii=False) + "\n")

    perfis = RAIZ / "familia" / "perfis.json"
    perfis.write_text(json.dumps({
        "regras": "R4, R27, R28, R36",
        "observacao": "A rede so entende. Quem bloqueia e o C#: intencao fora "
                      "da lista do perfil e recusada sem buscar dado nenhum.",
        "perfis": PERFIS,
        "modulos": {i: m for i, (m, _, _) in INTENCOES.items()},
        "rotulos": ROTULOS,
    }, ensure_ascii=False, indent=2), encoding="utf-8")

    n_bases = sum(len(b) for _, _, b in INTENCOES.values())
    print(f"{len(INTENCOES)} intencoes, {n_bases} frases-base, {len(linhas)} frases")
    print(f"gravado: {SAIDA.relative_to(RAIZ)} e {perfis.relative_to(RAIZ)}")
    print(f"perfil Crianca: {len(PERFIS['Crianca'])} de {len(INTENCOES)} intencoes")


if __name__ == "__main__":
    main()
