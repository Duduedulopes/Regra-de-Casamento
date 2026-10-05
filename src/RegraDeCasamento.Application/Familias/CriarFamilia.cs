using RegraDeCasamento.Application.Acesso;
using RegraDeCasamento.Application.Comum;
using RegraDeCasamento.Domain.Comum;
using RegraDeCasamento.Domain.Familias;
using RegraDeCasamento.Shared.Familias;

namespace RegraDeCasamento.Application.Familias;

/// <summary>Quem tem conta e ainda não tem família cria uma e vira o primeiro adulto dela.</summary>
public sealed class CriarFamilia(
    IUsuarioAtual usuario,
    IContasDeAcesso contas,
    IRepositorioDeFamilias familias,
    IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task<ResultadoDeDominio<FamiliaDto>> ExecutarAsync(DadosDaNovaFamilia dados, CancellationToken cancellationToken = default)
    {
        var contaId = usuario.ContaId ?? throw new InvalidOperationException("Criar família exige login.");

        if (await contas.ObterMembroIdAsync(contaId, cancellationToken) is not null)
        {
            return ResultadoDeDominio.Falha<FamiliaDto>("R1", "Esta conta já faz parte de uma família.");
        }

        var familia = Familia.Criar(dados.NomeDaFamilia, dados.SeuNome);
        familias.Adicionar(familia);
        await contas.VincularAoMembroAsync(contaId, familia.Membros.Single().Id, cancellationToken);
        await unidadeDeTrabalho.SalvarAsync(cancellationToken);

        return ResultadoDeDominio.Ok(MapeamentoDeFamilia.ParaDto(familia));
    }
}
