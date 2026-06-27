using System.Text.Json;
using CashManagement.Entries.Infrastructure.Messaging;
using CashManagement.Entries.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CashManagement.Entries.Api.Configuration;

/// <summary>
/// Health checks da API num só lugar (§7.1), com três endpoints de papéis distintos:
/// <list type="bullet">
/// <item><c>/health/live</c> — <b>liveness</b>: só responde que o processo está de pé (sem checar dependências).</item>
/// <item><c>/health/ready</c> — <b>readiness</b>: gateia tráfego; só o que a API <i>precisa</i> para servir (SQL).</item>
/// <item><c>/health</c> — <b>visão completa</b> das dependências (SQL + Kafka) para dashboards; <b>não</b> gateia.</item>
/// </list>
/// O Kafka fica fora do readiness de propósito: o Outbox desacopla a publicação do
/// caminho da request, então um Kafka fora não deve tirar a API do balanceador.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>Registra os checks: SQL (tag <c>ready</c>, gateia) e Kafka (tag <c>deps</c>, só visibilidade).</summary>
    public static IServiceCollection AddEntriesHealthChecks(this IServiceCollection services, string kafkaBootstrap)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<EntriesDbContext>("sql-server", tags: ["ready"])
            .AddCheck("kafka", new KafkaHealthCheck(kafkaBootstrap), tags: ["deps"]);
        return services;
    }

    /// <summary>Mapeia os três endpoints de saúde com seus predicados.</summary>
    public static WebApplication MapEntriesHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
        app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = WriteReport });
        return app;
    }

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
