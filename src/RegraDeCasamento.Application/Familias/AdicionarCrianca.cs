using RegraDeCasamento.Application.Acesso;
using RegraDeCasamento.Application.Comum;
using RegraDeCasamento.Domain.Comum;
using RegraDeCasamento.Shared.Acesso;
using RegraDeCasamento.Shared.Familias;

namespace RegraDeCasamento.Application.Familias;

/// <summary>
/// Um adulto cria a criança e a conta dela de uma vez (R35). A criança entra com "apelido@CODIGO" e o PIN.
/// </summary>
public sealed class AdicionarCrianca(
    IUsuarioAtual usuario,
    IContasDeAcesso contas,
    IRepositorioDeFamilias familias)
{
    public async Task<ResultadoDeDominio<CriancaCriadaDto>> ExecutarAsync(DadosDaNovaCrianca dados, CancellationToken cancellationToken = default)
    {
        // Só adultos chegam aqui (policy Adulto no servidor), então a família e o membro existem.
        var adultoId = usuario.MembroId ?? throw new InvalidOperationException("Criar criança exige um membro logado.");
        var familia = await familias.ObterDaFamiliaAtualAsync(cancellationToken)
            ?? throw new InvalidOperationException("Criar criança exige uma família.");

        var crianca = familia.AdicionarCrianca(adultoId, dados.Nome, dados.Apelido);
        if (crianca.Falhou)
        {
            return crianca.RepassarFalha<CriancaCriadaDto>();
        }

        var usuarioDaCrianca = DadosDeLoginDaCrianca.UsuarioDaCrianca(crianca.Valor.Apelido!, familia.Codigo);

        // Grava a criança e a conta juntas; PIN inválido lança DadosInvalidosException e nada é gravado.
        await contas.CriarContaDeCriancaAsync(usuarioDaCrianca, dados.Pin ?? "", crianca.Valor.Id, cancellationToken);

        return ResultadoDeDominio.Ok(new CriancaCriadaDto(crianca.Valor.Id, crianca.Valor.Nome, crianca.Valor.Apelido!, usuarioDaCrianca));
    }
}
