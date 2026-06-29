using System.Text.Json;
using CashManagement.Balance.Infrastructure.Persistence;
using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Shouldly;

namespace CashManagement.Balance.IntegrationTests;

/// <summary>
/// Integração ponta-a-ponta da fatia do Balance (T09): publica um envelope §4.3 no
/// Kafka, o consumer projeta no Mongo e o documento <c>DailyBalance</c> reflete o
/// crédito. Cobre também a idempotência: a mesma <c>event.id</c> entregue duas vezes
/// conta uma vez só (reprocesso é no-op).
/// </summary>
public class BalanceProjectionTests
    : IClassFixture<KafkaContainerFixture>, IClassFixture<MongoContainerFixture>
{
    private const string Topic = "cash.management.entries.events";

    private readonly KafkaContainerFixture _kafka;
    private readonly MongoContainerFixture _mongo;

    public BalanceProjectionTests(KafkaContainerFixture kafka, MongoContainerFixture mongo)
    {
        _kafka = kafka;
        _mongo = mongo;
    }

    [Fact]
    public async Task Projects_credit_and_is_idempotent_on_redelivery()
    {
        var database = $"balance-{Guid.NewGuid():N}";
        await using var factory = CreateHost(database);
        // força a construção do host → inicia o consumer (hosted service).
        _ = factory.Services;

        var collection = GetCollection(database);

        var eventId = Guid.NewGuid();
        var aggregateId = Guid.NewGuid();
        var occurredAt = new DateTime(2026, 6, 25, 23, 0, 0, DateTimeKind.Utc);
        var payload = BuildEnvelope(eventId, aggregateId, amount: 99.0m, occurredAt);

        // 1ª entrega: o crédito deve aparecer na projeção do dia.
        await ProduceAsync(aggregateId.ToString(), payload);

        var expectedDate = DateOnly.FromDateTime(occurredAt).ToString("yyyy-MM-dd");
        var afterFirst = await WaitForBalanceAsync(collection, expectedDate, d => d.TotalCredits == 99.0m);

        afterFirst.ShouldNotBeNull();
        afterFirst.TotalCredits.ShouldBe(99.0m);
        afterFirst.TotalDebits.ShouldBe(0m);
        afterFirst.Balance.ShouldBe(99.0m);
        afterFirst.ProcessedEventIds.ShouldContain(eventId.ToString());

        // 2ª entrega do MESMO event.id: reprocesso deve ser no-op (saldo inalterado).
        await ProduceAsync(aggregateId.ToString(), payload);

        // Dá tempo do consumer processar a 2ª mensagem antes de reler.
        await Task.Delay(TimeSpan.FromSeconds(3));
        var afterSecond = await collection.Find(d => d.Date == expectedDate).SingleAsync();

        afterSecond.TotalCredits.ShouldBe(99.0m); // continua contando 1×
        afterSecond.Balance.ShouldBe(99.0m);
        afterSecond.ProcessedEventIds.Count(id => id == eventId.ToString()).ShouldBe(1);
    }

    private WebApplicationFactory<Program> CreateHost(string database) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("Kafka:BootstrapServers", _kafka.BootstrapAddress)
                .UseSetting("Kafka:Topic", Topic)
                .UseSetting("Kafka:GroupId", $"balance-test-{Guid.NewGuid():N}")
                .UseSetting("Mongo:ConnectionString", _mongo.ConnectionString)
                .UseSetting("Mongo:Database", database)
                .UseSetting("Mongo:DailyBalanceCollection", "daily_balances"));

    private IMongoCollection<DailyBalanceDocument> GetCollection(string database) =>
        new MongoClient(_mongo.ConnectionString)
            .GetDatabase(database)
            .GetCollection<DailyBalanceDocument>("daily_balances");

    private static string BuildEnvelope(Guid eventId, Guid aggregateId, decimal amount, DateTime occurredAt)
    {
        var envelope = new
        {
            @event = new { id = eventId, type = "CreditPostedEvent", version = 1 },
            aggregate = new { id = aggregateId, version = 1 },
            correlationId = "corr-" + Guid.NewGuid(),
            initiatedBy = (string?)null,
            occurredAt,
            data = new
            {
                aggregateId,
                amount = new { amount, currency = "BRL" },
                occurredAt,
            },
        };

        return JsonSerializer.Serialize(envelope, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private async Task ProduceAsync(string key, string payload)
    {
        using var producer = new ProducerBuilder<string, string>(
            new ProducerConfig { BootstrapServers = _kafka.BootstrapAddress }).Build();

        await producer.ProduceAsync(Topic, new Message<string, string> { Key = key, Value = payload });
        producer.Flush(TimeSpan.FromSeconds(10));
    }

    private static async Task<DailyBalanceDocument?> WaitForBalanceAsync(
        IMongoCollection<DailyBalanceDocument> collection,
        string date,
        Func<DailyBalanceDocument, bool> predicate)
    {
        for (var attempt = 0; attempt < 40; attempt++)
        {
            var document = await collection.Find(d => d.Date == date).FirstOrDefaultAsync();
            if (document is not null && predicate(document))
            {
                return document;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        return null;
    }
}
