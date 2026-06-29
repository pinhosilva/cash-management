using System.Text.Json;
using CashManagement.Balance.Infrastructure.Persistence;
using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc.Testing;
using MongoDB.Driver;
using Shouldly;

namespace CashManagement.Balance.IntegrationTests;

/// <summary>
/// Integração da projeção de débito (espelha o crédito): publica um envelope §4.3 com
/// <c>event.type = "DebitPostedEvent"</c>, o consumer projeta no Mongo e o débito
/// <b>subtrai</b> do saldo (<c>balance = totalCredits - totalDebits</c>). Cobre o
/// cenário misto (crédito + débito no mesmo dia) e a idempotência por <c>event.id</c>.
/// </summary>
public class DebitProjectionTests
    : IClassFixture<KafkaContainerFixture>, IClassFixture<MongoContainerFixture>
{
    private const string Topic = "cash.management.entries.events";

    private readonly KafkaContainerFixture _kafka;
    private readonly MongoContainerFixture _mongo;

    public DebitProjectionTests(KafkaContainerFixture kafka, MongoContainerFixture mongo)
    {
        _kafka = kafka;
        _mongo = mongo;
    }

    [Fact]
    public async Task Projects_debit_and_subtracts_from_the_balance()
    {
        var database = $"balance-{Guid.NewGuid():N}";
        await using var factory = CreateHost(database);
        _ = factory.Services;

        var collection = GetCollection(database);
        var occurredAt = new DateTime(2026, 6, 25, 23, 0, 0, DateTimeKind.Utc);
        var date = DateOnly.FromDateTime(occurredAt).ToString("yyyy-MM-dd");

        await ProduceAsync(BuildEnvelope(Guid.NewGuid(), "DebitPostedEvent", amount: 30m, occurredAt));

        var doc = await WaitForBalanceAsync(collection, date, d => d.TotalDebits == 30m);

        doc.ShouldNotBeNull();
        doc.TotalCredits.ShouldBe(0m);
        doc.TotalDebits.ShouldBe(30m);
        doc.Balance.ShouldBe(-30m);
    }

    [Fact]
    public async Task Credit_and_debit_on_the_same_day_net_to_the_difference()
    {
        var database = $"balance-{Guid.NewGuid():N}";
        await using var factory = CreateHost(database);
        _ = factory.Services;

        var collection = GetCollection(database);
        var occurredAt = new DateTime(2026, 6, 26, 12, 0, 0, DateTimeKind.Utc);
        var date = DateOnly.FromDateTime(occurredAt).ToString("yyyy-MM-dd");

        await ProduceAsync(BuildEnvelope(Guid.NewGuid(), "CreditPostedEvent", amount: 100m, occurredAt));
        await ProduceAsync(BuildEnvelope(Guid.NewGuid(), "DebitPostedEvent", amount: 30m, occurredAt));

        var doc = await WaitForBalanceAsync(collection, date,
            d => d.TotalCredits == 100m && d.TotalDebits == 30m);

        doc.ShouldNotBeNull();
        doc.TotalCredits.ShouldBe(100m);
        doc.TotalDebits.ShouldBe(30m);
        doc.Balance.ShouldBe(70m);
    }

    [Fact]
    public async Task Debit_is_idempotent_on_redelivery()
    {
        var database = $"balance-{Guid.NewGuid():N}";
        await using var factory = CreateHost(database);
        _ = factory.Services;

        var collection = GetCollection(database);
        var eventId = Guid.NewGuid();
        var occurredAt = new DateTime(2026, 6, 27, 8, 0, 0, DateTimeKind.Utc);
        var date = DateOnly.FromDateTime(occurredAt).ToString("yyyy-MM-dd");
        var payload = BuildEnvelope(eventId, "DebitPostedEvent", amount: 45m, occurredAt);

        await ProduceAsync(payload);
        var afterFirst = await WaitForBalanceAsync(collection, date, d => d.TotalDebits == 45m);
        afterFirst.ShouldNotBeNull();

        await ProduceAsync(payload);
        await Task.Delay(TimeSpan.FromSeconds(3));
        var afterSecond = await collection.Find(d => d.Date == date).SingleAsync();

        afterSecond.TotalDebits.ShouldBe(45m);
        afterSecond.Balance.ShouldBe(-45m);
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

    private static string BuildEnvelope(Guid eventId, string type, decimal amount, DateTime occurredAt)
    {
        var aggregateId = Guid.NewGuid();
        var envelope = new
        {
            @event = new { id = eventId, type, version = 1 },
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

    private async Task ProduceAsync(string payload)
    {
        using var producer = new ProducerBuilder<string, string>(
            new ProducerConfig { BootstrapServers = _kafka.BootstrapAddress }).Build();

        await producer.ProduceAsync(Topic, new Message<string, string> { Key = Guid.NewGuid().ToString(), Value = payload });
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
