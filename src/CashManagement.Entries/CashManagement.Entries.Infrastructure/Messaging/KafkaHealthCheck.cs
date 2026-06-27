using Confluent.Kafka;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CashManagement.Entries.Infrastructure.Messaging;

/// <summary>
/// Health check de conectividade do Kafka: busca metadata do cluster (somente
/// leitura, com timeout) via <see cref="IAdminClient"/>. É um check de
/// <b>visibilidade</b> — <b>não</b> deve gatear o readiness, pois o Outbox desacopla
/// a publicação do caminho da request (§7.1): a API continua aceitando lançamentos
/// com o Kafka fora; o relay reenvia quando ele volta.
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
