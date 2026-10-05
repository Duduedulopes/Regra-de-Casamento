using RegraDeCasamento.Domain.Familias;

namespace RegraDeCasamento.Domain.Tests.Financas;

/// <summary>Uma família pronta para os testes de dinheiro: Ana e Bruno (adultos) e Lia (criança).</summary>
internal sealed class Casal
{
    public Casal()
    {
        Familia = Familia.Criar("Família Silva", "Ana");
        Ana = Familia.Membros.Single();
        var pedido = Familia.ReceberPedidoDeEntrada(Guid.CreateVersion7(), "Bruno").Valor;
        Bruno = Familia.AprovarPedidoDeEntrada(pedido.Id, Ana.Id).Valor;
        Lia = Familia.AdicionarCrianca(Ana.Id, "Lia", "lia").Valor;
    }

    public Familia Familia { get; }

    public Membro Ana { get; }

    public Membro Bruno { get; }

    public Membro Lia { get; }

    public static DateOnly Dia(int dia, int mes = 10, int ano = 2026) => new(ano, mes, dia);
}
