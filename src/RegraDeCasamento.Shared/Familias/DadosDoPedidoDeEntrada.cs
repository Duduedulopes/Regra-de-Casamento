namespace RegraDeCasamento.Shared.Familias;

/// <summary>Pedido para entrar numa família, com o código que um adulto dela passou (R34).</summary>
public sealed record DadosDoPedidoDeEntrada(string Codigo, string SeuNome);

/// <summary>O pedido foi enviado e espera a resposta de um adulto da família.</summary>
public sealed record PedidoEnviadoDto(Guid PedidoId);
