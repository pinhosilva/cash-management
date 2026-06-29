using CashManagement.Balance.Application.Features.GetDailyBalance;
using MongoDB.Driver;

namespace CashManagement.Balance.Infrastructure.Persistence;

/// <summary>
/// Implementação <b>somente leitura</b> da porta <see cref="IDailyBalanceReader"/>:
/// lê o documento do dia da coleção <c>daily_balances</c> por <c>_id</c>
/// (<c>yyyy-MM-dd</c>) e mapeia para o DTO do read model. Responde direto da projeção
/// Mongo — o Balance nunca consulta o Entries (§4.2).
/// </summary>
public sealed class MongoDailyBalanceReader : IDailyBalanceReader
{
    private readonly IMongoCollection<DailyBalanceDocument> _collection;

    public MongoDailyBalanceReader(IMongoCollection<DailyBalanceDocument> collection) =>
        _collection = collection;

    public async Task<DailyBalanceDto?> GetAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var key = date.ToString("yyyy-MM-dd");

        var document = await _collection
            .Find(d => d.Date == key)
            .FirstOrDefaultAsync(cancellationToken);

        return document is null
            ? null
            : new DailyBalanceDto(date, document.TotalCredits, document.TotalDebits, document.Balance);
    }
}
