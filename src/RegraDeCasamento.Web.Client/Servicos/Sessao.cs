using RegraDeCasamento.Shared.Acesso;

namespace RegraDeCasamento.Web.Client.Servicos;

/// <summary>
/// Quem está usando o app neste aparelho, lido de <c>/api/acesso/eu</c>. Serve para a tela escolher o que mostrar;
/// quem confere a permissão de verdade é sempre o servidor.
/// </summary>
public sealed class Sessao(ApiDoApp api)
{
    private bool _carregada;

    public event Action? Mudou;

    public MeuAcessoDto? Atual { get; private set; }

    public bool Logado => Atual is not null;

    public bool TemFamilia => Atual?.FamiliaId is not null;

    public bool EhAdulto => Atual?.Perfil == PerfilDeAcesso.Adulto;

    /// <summary>O nome a mostrar: o e-mail do adulto, ou só o apelido da criança (sem o "@CODIGO").</summary>
    public string NomeParaMostrar =>
        Atual is null ? "" : EhAdulto || !TemFamilia ? Atual.Usuario : Atual.Usuario.Split('@')[0];

    public async Task<MeuAcessoDto?> ObterAsync()
    {
        if (!_carregada)
        {
            await RecarregarAsync();
        }

        return Atual;
    }

    public async Task RecarregarAsync()
    {
        Atual = await api.EuAsync();
        _carregada = true;
        Mudou?.Invoke();
    }

    public async Task SairAsync()
    {
        await api.SairAsync();
        Atual = null;
        _carregada = true;
        Mudou?.Invoke();
    }
}
