using RegraDeCasamento.Application.Acesso;
using RegraDeCasamento.Application.Comum;
using RegraDeCasamento.Application.Familias;
using RegraDeCasamento.Domain.Comum;
using RegraDeCasamento.Domain.Familias;

namespace RegraDeCasamento.Application.Tests;

/// <summary>Famílias guardadas em memória. "Família atual" é a do membro logado, como faz o filtro do banco.</summary>
internal sealed class FamiliasEmMemoria(UsuarioFixo usuario) : IRepositorioDeFamilias
{
    public List<Familia> Familias { get; } = [];

    public Task<Familia?> ObterDaFamiliaAtualAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Familias.SingleOrDefault(familia => familia.Membros.Any(membro => membro.Id == usuario.MembroId)));

    public Task<Familia?> ObterPeloCodigoAsync(string codigo, CancellationToken cancellationToken = default) =>
        Task.FromResult(Familias.SingleOrDefault(familia => familia.Codigo == Familia.NormalizarCodigo(codigo)));

    public void Adicionar(Familia familia) => Familias.Add(familia);
}

/// <summary>Contas em memória: só o vínculo conta → membro, e a lista de contas criadas para crianças.</summary>
internal sealed class ContasEmMemoria : IContasDeAcesso
{
    public Dictionary<Guid, Guid> MembroDaConta { get; } = [];

    public List<(string Usuario, Guid MembroId)> ContasCriadas { get; } = [];

    public string? SenhaRecusada { get; set; }

    public Task<Guid?> ObterMembroIdAsync(Guid contaId, CancellationToken cancellationToken = default) =>
        Task.FromResult(MembroDaConta.TryGetValue(contaId, out var membroId) ? membroId : (Guid?)null);

    public Task VincularAoMembroAsync(Guid contaId, Guid membroId, CancellationToken cancellationToken = default)
    {
        MembroDaConta[contaId] = membroId;
        return Task.CompletedTask;
    }

    public Task CriarContaDeCriancaAsync(string usuario, string pin, Guid membroId, CancellationToken cancellationToken = default)
    {
        if (pin == SenhaRecusada)
        {
            throw new DadosInvalidosException("O PIN precisa ter de 4 a 6 números.");
        }

        ContasCriadas.Add((usuario, membroId));
        return Task.CompletedTask;
    }
}

internal sealed class UsuarioFixo : IUsuarioAtual
{
    public Guid? ContaId { get; set; } = Guid.CreateVersion7();

    public Guid? MembroId { get; set; }
}

internal sealed class UnidadeDeTrabalhoQueConta : IUnidadeDeTrabalho
{
    public int VezesQueSalvou { get; private set; }

    public Task SalvarAsync(CancellationToken cancellationToken = default)
    {
        VezesQueSalvou++;
        return Task.CompletedTask;
    }
}
