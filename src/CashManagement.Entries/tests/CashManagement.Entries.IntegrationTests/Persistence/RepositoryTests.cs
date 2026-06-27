using System.Text.Json;
using CashManagement.Entries.Domain.Aggregates;
using CashManagement.Entries.Domain.ValueObjects;
using CashManagement.Entries.Infrastructure.Persistence;
using CashManagement.Entries.Infrastructure.Serialization;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace CashManagement.Entries.IntegrationTests.Persistence;

public class RepositoryTests : IClassFixture<MsSqlContainerFixture>, IAsyncLifetime
{
    private readonly MsSqlContainerFixture _fixture;
    private EntriesDbContext _db = default!;
    private Repository _repository = default!;
    private UnitOfWork _unitOfWork = default!;

    public RepositoryTests(MsSqlContainerFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _db = CreateDbContext();
        await _db.Database.EnsureCreatedAsync();
        _repository = new Repository(_db, new EventSerializer());
        _unitOfWork = new UnitOfWork(_db);
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
    public async Task Add_and_commit_persists_event_and_outbox_atomically()
    {
        var id = Guid.NewGuid();

        _repository.Add(Entry.PostCredit(id, Money.Of(150m, "BRL"), DateTime.UtcNow));
        await _unitOfWork.CommitAsync();

        await using var verify = CreateDbContext();
        (await verify.Events.CountAsync(e => e.AggregateId == id)).ShouldBe(1);
        (await verify.Outbox.CountAsync(o => o.AggregateId == id)).ShouldBe(1);
    }

    [Fact]
    public async Task GetAsync_rebuilds_entry_by_replay()
    {
        var id = Guid.NewGuid();
        _repository.Add(Entry.PostCredit(id, Money.Of(200m, "BRL"), DateTime.UtcNow));
        await _unitOfWork.CommitAsync();

        await using var read = CreateDbContext();
        var repository = new Repository(read, new EventSerializer());
        var entry = await repository.GetAsync<Entry>(id);

        entry.ShouldNotBeNull();
        entry!.Id.ShouldBe(id);
        entry.Type.ShouldBe(EntryType.Credit);
        entry.Amount.ShouldBe(Money.Of(200m, "BRL"));
        entry.Version.ShouldBe(1);
    }

    [Fact]
    public async Task Committing_a_conflicting_version_throws_concurrency_conflict()
    {
        var id = Guid.NewGuid();
        _repository.Add(Entry.PostCredit(id, Money.Of(10m, "BRL"), DateTime.UtcNow));
        await _unitOfWork.CommitAsync();

        await using var second = CreateDbContext();
        var repository = new Repository(second, new EventSerializer());
        var unitOfWork = new UnitOfWork(second);
        repository.Add(Entry.PostCredit(id, Money.Of(20m, "BRL"), DateTime.UtcNow)); // mesmo id => versão 1 de novo

        await Should.ThrowAsync<ConcurrencyConflictException>(() => unitOfWork.CommitAsync());
    }

    [Fact]
    public async Task Outbox_envelope_follows_the_contract()
    {
        var id = Guid.NewGuid();
        _repository.Add(Entry.PostCredit(id, Money.Of(99m, "BRL"), DateTime.UtcNow));
        await _unitOfWork.CommitAsync();

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
