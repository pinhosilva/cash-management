using CashManagement.Entries.Application.Interfaces;
using CashManagement.Entries.Domain.Aggregates;
using CashManagement.Entries.Domain.ValueObjects;
using CashManagement.Entries.Infrastructure.Messaging;
using CashManagement.Entries.Infrastructure.Persistence;
using CashManagement.Entries.Infrastructure.Serialization;
using CashManagement.Entries.IntegrationTests.Persistence;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace CashManagement.Entries.IntegrationTests.Messaging;

public class OutboxProcessorTests
    : IClassFixture<MsSqlContainerFixture>, IClassFixture<KafkaContainerFixture>, IAsyncLifetime
{
    private const string Topic = "cash.management.entries.events";

    private readonly MsSqlContainerFixture _sql;
    private readonly KafkaContainerFixture _kafka;
    private EntriesDbContext _db = default!;

    public OutboxProcessorTests(MsSqlContainerFixture sql, KafkaContainerFixture kafka)
    {
        _sql = sql;
        _kafka = kafka;
    }

    public async Task InitializeAsync()
    {
        _db = CreateDbContext();
        await _db.Database.EnsureCreatedAsync();
        // isolamento: os testes da classe compartilham o mesmo banco
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM [Outbox]; DELETE FROM [Events];");
    }

    public Task DisposeAsync()
    {
        _db.Dispose();
        return Task.CompletedTask;
    }

    private EntriesDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<EntriesDbContext>().UseSqlServer(_sql.ConnectionString).Options);

    private async Task<Guid> SeedPendingOutboxAsync()
    {
        var id = Guid.NewGuid();
        new Repository(_db, new EventSerializer()).Add(Entry.PostCredit(id, Money.Of(50m, "BRL"), DateTime.UtcNow));
        await new UnitOfWork(_db).CommitAsync();
        return id;
    }

    [Fact]
    public async Task Publishes_pending_outbox_to_kafka_and_marks_processed()
    {
        var id = await SeedPendingOutboxAsync();
        using var publisher = new KafkaEventPublisher(_kafka.BootstrapAddress);
        var processor = new OutboxProcessor(_db, publisher);

        await processor.ProcessPendingAsync();

        await using var verify = CreateDbContext();
        (await verify.Outbox.SingleAsync(o => o.AggregateId == id)).ProcessedAt.ShouldNotBeNull();

        var payload = ConsumeValueByKey(_kafka.BootstrapAddress, Topic, id.ToString());
        payload.ShouldContain(id.ToString()); // envelope §4.3 carrega aggregate.id
    }

    [Fact]
    public async Task Leaves_row_unprocessed_when_publish_fails()
    {
        var id = await SeedPendingOutboxAsync();
        var processor = new OutboxProcessor(_db, new ThrowingPublisher());

        await processor.ProcessPendingAsync();

        await using var verify = CreateDbContext();
        (await verify.Outbox.SingleAsync(o => o.AggregateId == id)).ProcessedAt.ShouldBeNull();
    }

    private static string ConsumeValueByKey(string bootstrap, string topic, string expectedKey)
    {
        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = "test-" + Guid.NewGuid(),
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
        }).Build();

        consumer.Subscribe(topic);
        try
        {
            for (var attempt = 0; attempt < 30; attempt++)
            {
                var result = consumer.Consume(TimeSpan.FromSeconds(1));
                if (result?.Message.Key == expectedKey)
                {
                    return result.Message.Value;
                }
            }
        }
        finally
        {
            consumer.Close();
        }

        throw new Xunit.Sdk.XunitException($"No message with key '{expectedKey}' consumed within the timeout.");
    }

    private sealed class ThrowingPublisher : IEventPublisher
    {
        public Task PublishAsync(string key, string payload, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("publish failed");
    }
}
