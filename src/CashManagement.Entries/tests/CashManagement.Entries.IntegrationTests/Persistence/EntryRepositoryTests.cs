using System.Text.Json;
using CashManagement.Entries.Domain.Aggregates;
using CashManagement.Entries.Domain.ValueObjects;
using CashManagement.Entries.Infrastructure.Persistence;
using CashManagement.Entries.Infrastructure.Repositories;
using CashManagement.Entries.Infrastructure.Serialization;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace CashManagement.Entries.IntegrationTests.Persistence;

public class EntryRepositoryTests : IClassFixture<MsSqlContainerFixture>, IAsyncLifetime
{
    private readonly MsSqlContainerFixture _fixture;
    private EntriesDbContext _db = default!;
    private EntryRepository _sut = default!;

    public EntryRepositoryTests(MsSqlContainerFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _db = CreateDbContext();
        await _db.Database.EnsureCreatedAsync();
        _sut = new EntryRepository(_db, new EventSerializer());
    }

    public Task DisposeAsync()
    {
        _db.Dispose();
        return Task.CompletedTask;
    }

    private EntriesDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EntriesDbContext>()
            .UseSqlServer(_fixture.ConnectionString)
            .Options;
        return new EntriesDbContext(options);
    }

    [Fact]
    public async Task SaveAsync_persists_event_and_outbox_atomically()
    {
        var id = Guid.NewGuid();
        var entry = Entry.PostCredit(id, Money.Of(150m, "BRL"), DateTime.UtcNow);

        await _sut.SaveAsync(entry);

        await using var verify = CreateDbContext();
        (await verify.Events.CountAsync(e => e.AggregateId == id)).ShouldBe(1);
        (await verify.Outbox.CountAsync(o => o.AggregateId == id)).ShouldBe(1);
    }

    [Fact]
    public async Task GetByIdAsync_rebuilds_entry_by_replay()
    {
        var id = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow;
        await _sut.SaveAsync(Entry.PostCredit(id, Money.Of(200m, "BRL"), occurredAt));

        await using var read = CreateDbContext();
        var repo = new EntryRepository(read, new EventSerializer());
        var entry = await repo.GetByIdAsync(id);

        entry.ShouldNotBeNull();
        entry!.Id.ShouldBe(id);
        entry.Type.ShouldBe(EntryType.Credit);
        entry.Amount.ShouldBe(Money.Of(200m, "BRL"));
        entry.Version.ShouldBe(1);
    }

    [Fact]
    public async Task SaveAsync_with_conflicting_version_throws_concurrency_conflict()
    {
        var id = Guid.NewGuid();
        await _sut.SaveAsync(Entry.PostCredit(id, Money.Of(10m, "BRL"), DateTime.UtcNow));

        await using var second = CreateDbContext();
        var repo = new EntryRepository(second, new EventSerializer());
        var conflicting = Entry.PostCredit(id, Money.Of(20m, "BRL"), DateTime.UtcNow); // mesmo id => versão 1 de novo

        await Should.ThrowAsync<ConcurrencyConflictException>(() => repo.SaveAsync(conflicting));
    }

    [Fact]
    public async Task SaveAsync_writes_outbox_envelope_following_the_contract()
    {
        var id = Guid.NewGuid();
        await _sut.SaveAsync(Entry.PostCredit(id, Money.Of(99m, "BRL"), DateTime.UtcNow));

        await using var verify = CreateDbContext();
        var message = await verify.Outbox.SingleAsync(o => o.AggregateId == id);
        using var doc = JsonDocument.Parse(message.Payload);
        var root = doc.RootElement;

        root.GetProperty("event").GetProperty("id").GetGuid().ShouldNotBe(Guid.Empty);
        root.GetProperty("event").GetProperty("type").GetString().ShouldBe("CreditPostedEvent");
        root.GetProperty("aggregate").GetProperty("id").GetGuid().ShouldBe(id);
        root.GetProperty("aggregate").GetProperty("version").GetInt32().ShouldBe(1);
        root.TryGetProperty("occurredAt", out _).ShouldBeTrue();
        root.GetProperty("data").GetProperty("amount").GetProperty("amount").GetDecimal().ShouldBe(99m);
        root.GetProperty("data").GetProperty("amount").GetProperty("currency").GetString().ShouldBe("BRL");
    }
}
