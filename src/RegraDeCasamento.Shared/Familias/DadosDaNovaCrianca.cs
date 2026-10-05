namespace RegraDeCasamento.Shared.Familias;

/// <summary>Um adulto cria a conta da criança (R35). O PIN tem de 4 a 6 números.</summary>
public sealed record DadosDaNovaCrianca(string Nome, string Apelido, string Pin);

/// <summary>A criança foi criada. <see cref="Usuario"/> é o que ela usa para entrar (ex.: "lia@K7P2QXAB").</summary>
public sealed record CriancaCriadaDto(Guid MembroId, string Nome, string Apelido, string Usuario);
