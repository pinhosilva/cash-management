using CashManagement.Balance.Application.Features.BalanceProjection;
using MongoDB.Driver;

namespace CashManagement.Balance.Infrastructure.Persistence;

/// <summary>
/// Projeção do saldo diário no MongoDB com <b>exactly-once de efeito</b>: o marcador
/// de dedup (<c>event.id</c>) e o ajuste do saldo vão no <b>mesmo documento</b>, num
/// <b>único update condicional atômico</b> (FAQ #5, §6.3) — sem dual-write.
///
/// O filtro exige que a <c>event.id</c> ainda <b>não</b> esteja em
/// <c>processedEventIds</c>; com <c>upsert</c>, o <c>$inc</c> ajusta os totais e o
/// <c>$push</c> registra a <c>event.id</c>. Reprocessar a mesma <c>event.id</c> não
/// casa o filtro (o documento já existe com a id) → 0 modificações → no-op.
/// </summary>
public sealed class MongoDailyBalanceProjection : IDailyBalanceProjection
{
    private readonly IMongoCollection<DailyBalanceDocument> _collection;

    public MongoDailyBalanceProjection(IMongoCollection<DailyBalanceDocument> collection) =>
        _collection = collection;

    public async Task<bool> ApplyAsync(string eventId, DateOnly date, decimal amount, EntryKind kind, CancellationToken cancellationToken = default)
    {
        var key = date.ToString("yyyy-MM-dd");

        // Filtro: o dia certo E a event.id ainda NÃO aplicada (array sem o id; cobre
        // também o documento inexistente/array vazio). É o que torna o reprocesso no-op.
        var filter = Builders<DailyBalanceDocument>.Filter.And(
            Builders<DailyBalanceDocument>.Filter.Eq(d => d.Date, key),
            Builders<DailyBalanceDocument>.Filter.Not(
                Builders<DailyBalanceDocument>.Filter.AnyEq(d => d.ProcessedEventIds, eventId)));

        // Crédito soma; débito subtrai. O ajuste do total (do lado certo) e do balance
        // entram no MESMO update condicional do crédito — um único $inc por campo,
        // mantendo a dedup atômica (sem dual-write). balance = totalCredits - totalDebits.
        var builder = Builders<DailyBalanceDocument>.Update;
        var update = kind == EntryKind.Debit
            ? builder
                .Inc(d => d.TotalDebits, amount)
                .Inc(d => d.Balance, -amount)
                .SetOnInsert(d => d.TotalCredits, 0m)
                .Push(d => d.ProcessedEventIds, eventId)
            : builder
                .Inc(d => d.TotalCredits, amount)
                .Inc(d => d.Balance, amount)
                .SetOnInsert(d => d.TotalDebits, 0m)
                .Push(d => d.ProcessedEventIds, eventId);

        try
        {
            var result = await _collection.UpdateOneAsync(
                filter,
                update,
                new UpdateOptions { IsUpsert = true },
                cancellationToken);

            // Inseriu (1ª vez no dia) ou modificou (dia existente, id nova) ⇒ aplicado.
            return result.UpsertedId is not null || result.ModifiedCount > 0;
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            // Corrida no upsert do mesmo dia: outro consumo inseriu o documento entre o
            // filtro e a escrita. A event.id já está aplicada por aquele caminho → no-op.
            return false;
        }
    }
}
