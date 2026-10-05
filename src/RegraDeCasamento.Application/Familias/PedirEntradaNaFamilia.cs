using RegraDeCasamento.Application.Acesso;
using RegraDeCasamento.Application.Comum;
using RegraDeCasamento.Domain.Comum;
using RegraDeCasamento.Shared.Familias;

namespace RegraDeCasamento.Application.Familias;

/// <summary>
/// Uma conta sem família pede para entrar numa, com o código (R34). Quem pede só recebe o Id do pedido:
/// nada da família é devolvido antes da aprovação (R5).
/// </summary>
public sealed class PedirEntradaNaFamilia(
    IUsuarioAtual usuario,
    IContasDeAcesso contas,
    IRepositorioDeFamilias familias,
    IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task<ResultadoDeDominio<PedidoEnviadoDto>> ExecutarAsync(DadosDoPedidoDeEntrada dados, CancellationToken cancellationToken = default)
    {
        var contaId = usuario.ContaId ?? throw new InvalidOperationException("Pedir entrada exige login.");

        if (await contas.ObterMembroIdAsync(contaId, cancellationToken) is not null)
        {
            return ResultadoDeDominio.Falha<PedidoEnviadoDto>("R1", "Esta conta já faz parte de uma família.");
        }

        var familia = await familias.ObterPeloCodigoAsync(dados.Codigo ?? "", cancellationToken);
        if (familia is null)
        {
            return ResultadoDeDominio.Falha<PedidoEnviadoDto>("R34", "Não existe família com este código. Confira com quem passou o código.");
        }

        var pedido = familia.ReceberPedidoDeEntrada(contaId, dados.SeuNome);
        if (pedido.Falhou)
        {
            return pedido.RepassarFalha<PedidoEnviadoDto>();
        }

        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        return ResultadoDeDominio.Ok(new PedidoEnviadoDto(pedido.Valor.Id));
    }
}
