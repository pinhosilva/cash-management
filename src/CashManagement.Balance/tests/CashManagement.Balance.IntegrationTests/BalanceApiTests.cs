using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CashManagement.Balance.Api.Auth;
using CashManagement.Balance.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Shouldly;

namespace CashManagement.Balance.IntegrationTests;

/// <summary>
/// Integração do endpoint <c>GET /balances/{date}</c> (T10): a consulta é servida
/// direto da projeção Mongo (semeada no teste), com JWT (scope <c>balances:read</c>).
/// Cobre o caminho feliz (200 + envelope do read model), o 401 sem token e o 404
/// quando não há projeção para o dia. Não depende do consumer Kafka — o Kafka aponta
/// para um endereço inexistente e o documento é inserido direto no Mongo.
/// </summary>
public class BalanceApiTests : IClassFixture<MongoContainerFixture>
{
    private readonly MongoContainerFixture _mongo;

    public BalanceApiTests(MongoContainerFixture mongo) => _mongo = mongo;

    [Fact]
    public async Task Returns_200_with_seeded_projection_when_token_is_valid()
    {
        var database = $"balance-api-{Guid.NewGuid():N}";
        await using var factory = CreateHost(database);

        await SeedAsync(database, new DailyBalanceDocument
        {
            Date = "2026-06-25",
            TotalCredits = 150.50m,
            TotalDebits = 0m,
            Balance = 150.50m,
            ProcessedEventIds = [Guid.NewGuid().ToString()],
        });

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(factory));

        using var response = await client.GetAsync("/balances/2026-06-25");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        root.GetProperty("status").GetString().ShouldBe("success");
        root.GetProperty("correlationId").GetString().ShouldNotBeNullOrWhiteSpace();

        var result = root.GetProperty("result");
        result.GetProperty("date").GetString().ShouldBe("2026-06-25");
        result.GetProperty("totalCredits").GetDecimal().ShouldBe(150.50m);
        result.GetProperty("totalDebits").GetDecimal().ShouldBe(0m);
        result.GetProperty("balance").GetDecimal().ShouldBe(150.50m);
    }

    [Fact]
    public async Task Returns_401_with_error_envelope_when_token_is_missing()
    {
        var database = $"balance-api-{Guid.NewGuid():N}";
        await using var factory = CreateHost(database);

        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/balances/2026-06-25");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        root.GetProperty("status").GetString().ShouldBe("error");
        root.GetProperty("error").GetProperty("code").GetString().ShouldBe("UNAUTHORIZED");
    }

    [Fact]
    public async Task Returns_404_when_no_projection_exists_for_the_date()
    {
        var database = $"balance-api-{Guid.NewGuid():N}";
        await using var factory = CreateHost(database);

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(factory));

        using var response = await client.GetAsync("/balances/2030-01-01");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        root.GetProperty("status").GetString().ShouldBe("error");
        root.GetProperty("error").GetProperty("code").GetString().ShouldBe("NOT_FOUND");
    }

    [Fact]
    public async Task Returns_400_when_date_format_is_invalid()
    {
        var database = $"balance-api-{Guid.NewGuid():N}";
        await using var factory = CreateHost(database);

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(factory));

        using var response = await client.GetAsync("/balances/25-06-2026");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("error").GetProperty("code").GetString().ShouldBe("VALIDATION_FAILED");
    }

    private WebApplicationFactory<Program> CreateHost(string database) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("Mongo:ConnectionString", _mongo.ConnectionString)
                .UseSetting("Mongo:Database", database)
                .UseSetting("Mongo:DailyBalanceCollection", "daily_balances")
                // Kafka aponta para um endereço inexistente: o consumer (hosted service)
                // não derruba o host e esta suíte não exercita o caminho de consumo.
                .UseSetting("Kafka:BootstrapServers", "localhost:1")
                .UseSetting("Kafka:Topic", "cash.management.entries.events")
                .UseSetting("Kafka:GroupId", $"balance-api-test-{Guid.NewGuid():N}"));

    private static string IssueToken(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var tokens = scope.ServiceProvider.GetRequiredService<DevTokenService>();
        return tokens.IssueReadToken();
    }

    private async Task SeedAsync(string database, DailyBalanceDocument document)
    {
        var collection = new MongoClient(_mongo.ConnectionString)
            .GetDatabase(database)
            .GetCollection<DailyBalanceDocument>("daily_balances");

        await collection.InsertOneAsync(document);
    }
}
