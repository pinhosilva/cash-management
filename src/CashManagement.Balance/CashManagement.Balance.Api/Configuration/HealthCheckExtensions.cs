using System.Text.Json;
using CashManagement.Balance.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace CashManagement.Balance.Api.Configuration;

/// <summary>
/// Health checks da API num só lugar (§7.1), com três endpoints de papéis distintos:
/// <list type="bullet">
/// <item><c>/health/live</c> — <b>liveness</b>: só responde que o processo está de pé (sem checar dependências).</item>
/// <item><c>/health/ready</c> — <b>readiness</b>: gateia tráfego; só o que a API <i>precisa</i> para servir (Mongo).</item>
/// <item><c>/health</c> — <b>visão completa</b> das dependências (Mongo + Kafka) para dashboards; <b>não</b> gateia.</item>
/// </list>
/// <b>Divergência consciente com a §7.1</b> (que lista "Balance: MongoDB + Kafka" no
/// readiness): aplicando a mesma lógica do Entries, o Kafka fica <b>fora</b> do
/// readiness. O Kafka alimenta o consumer assíncrono; a consulta de saldo é servida
/// direto do Mongo, então um Kafka fora não deve tirar a API do balanceador. O status
/// do Kafka aparece só em <c>/health</c> (visibilidade). Anotado no relatório da T10.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>Registra os checks: Mongo (tag <c>ready</c>, gateia) e Kafka (tag <c>deps</c>, só visibilidade).</summary>
    public static IServiceCollection AddBalanceHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var kafkaBootstrap = configuration.GetSection(KafkaSection)["BootstrapServers"] ?? DefaultKafkaBootstrap;

        services.AddHealthChecks()
            .AddCheck<MongoReadinessCheck>("mongodb", tags: ["ready"])
            .AddCheck("kafka", new KafkaHealthCheck(kafkaBootstrap), tags: ["deps"]);

        return services;
    }

    /// <summary>Mapeia os três endpoints de saúde com seus predicados.</summary>
    public static WebApplication MapBalanceHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
        app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = WriteReport });
        return app;
    }

    private const string KafkaSection = "Kafka";
    private const string DefaultKafkaBootstrap = "localhost:9092";

    private static async Task WriteReport(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
            }),
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }
}

/// <summary>
/// Adaptador que resolve <see cref="MongoHealthCheck"/> com o nome do banco das
/// <see cref="MongoOptions"/>, para o registro via <c>AddCheck&lt;T&gt;</c> (DI).
/// </summary>
internal sealed class MongoReadinessCheck : IHealthCheck
{
    private readonly MongoHealthCheck _inner;

    public MongoReadinessCheck(IMongoClient client, IOptions<MongoOptions> options) =>
        _inner = new MongoHealthCheck(client, options.Value.Database);

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        _inner.CheckHealthAsync(context, cancellationToken);
}
