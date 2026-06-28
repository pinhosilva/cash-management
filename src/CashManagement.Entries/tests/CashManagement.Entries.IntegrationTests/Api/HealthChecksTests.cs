using System.Net;
using System.Text.Json;
using Shouldly;
using Xunit;

namespace CashManagement.Entries.IntegrationTests.Api;

/// <summary>
/// Testes dos endpoints de saúde (§7.1): liveness, readiness (gateia, só SQL) e a
/// visão completa (SQL + Kafka, sem gatear). A fixture aponta o Kafka para um broker
/// inexistente — o que evidencia que o readiness <b>não</b> depende dele.
/// </summary>
public class HealthChecksTests : IClassFixture<EntriesApiFixture>
{
    private readonly EntriesApiFixture _fixture;

    public HealthChecksTests(EntriesApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Liveness_is_healthy_without_checking_dependencies()
    {
        var response = await _fixture.Client.GetAsync("/health/live");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Readiness_is_healthy_when_sql_is_up_even_with_kafka_down()
    {
        var response = await _fixture.Client.GetAsync("/health/ready");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Full_health_reports_both_sql_and_kafka()
    {
        var response = await _fixture.Client.GetAsync("/health");

        var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var checks = doc.RootElement.GetProperty("checks").EnumerateArray()
            .Select(check => check.GetProperty("name").GetString())
            .ToList();

        checks.ShouldContain("sql-server");
        checks.ShouldContain("kafka");
    }
}
