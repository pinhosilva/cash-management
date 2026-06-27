namespace CashManagement.Entries.Application.Interfaces;

/// <summary>
/// Persistência das chaves de idempotência de escrita (§4.3). A borda da API
/// consulta antes de processar (retry devolve o id original) e registra após a
/// primeira gravação bem-sucedida.
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>Devolve o id do lançamento já criado para a chave, ou <c>null</c>.</summary>
    Task<Guid?> FindAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Registra a chave com o id criado. Não commita (faz parte da transação do caso de uso).</summary>
    void Add(string key, Guid entryId);
}
