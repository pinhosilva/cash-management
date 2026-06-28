using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CashManagement.Balance.Api.Configuration;

/// <summary>
/// Check de saúde do MongoDB (§7.1): dá um <c>ping</c> no servidor (comando
/// <c>{ ping: 1 }</c>) com o timeout do <see cref="HealthCheckContext"/>. É a
/// dependência que o Balance <b>precisa</b> para servir a consulta de saldo, então
/// entra no <c>readiness</c>. Réplica da ideia do check próprio do Entries.
/// </summary>
public sealed class MongoHealthCheck : IHealthCheck
{
    private readonly IMongoClient _client;
    private readonly string _database;

    public MongoHealthCheck(IMongoClient client, string database)
    {
        _client = client;
        _database = database;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var database = _client.GetDatabase(_database);
            await database.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1), cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MongoDB is not reachable.", ex);
        }
    }
}
