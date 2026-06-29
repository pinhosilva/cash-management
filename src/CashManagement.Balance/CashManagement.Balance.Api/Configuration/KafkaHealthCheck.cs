using Confluent.Kafka;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CashManagement.Balance.Api.Configuration;

/// <summary>
/// Health check de conectividade do Kafka: busca metadata do cluster (somente
/// leitura, com timeout) via <see cref="IAdminClient"/>. No Balance é um check de
/// <b>visibilidade</b> (aparece em <c>/health</c>) e <b>não</b> gateia o readiness:
/// o Kafka alimenta o consumer assíncrono, não a consulta de saldo — que é servida
/// direto do Mongo. Réplica da ideia do check do Entries.
/// </summary>
public sealed class KafkaHealthCheck : IHealthCheck
{
    private static readonly TimeSpan MetadataTimeout = TimeSpan.FromSeconds(3);

    private readonly string _bootstrapServers;

    public KafkaHealthCheck(string bootstrapServers) => _bootstrapServers = bootstrapServers;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var admin = new AdminClientBuilder(
                new AdminClientConfig { BootstrapServers = _bootstrapServers }).Build();
            admin.GetMetadata(MetadataTimeout);
            return Task.FromResult(HealthCheckResult.Healthy());
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Kafka unreachable.", ex));
        }
    }
}
