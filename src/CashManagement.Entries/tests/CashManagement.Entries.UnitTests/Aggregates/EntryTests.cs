using CashManagement.Entries.Domain.Aggregates;
using CashManagement.Entries.Domain.Events;
using CashManagement.Entries.Domain.SeedWork;
using CashManagement.Entries.Domain.ValueObjects;
using CashManagement.Entries.UnitTests.Fixtures;
using Shouldly;
using Xunit;

namespace CashManagement.Entries.UnitTests.Aggregates;

public class When_posting_a_credit : AggregateTestFixture<Entry>
{
    private readonly Guid _id = Guid.NewGuid();
    private readonly DateTime _occurredAt = DateTime.UtcNow;

    protected override Entry When() => Entry.PostCredit(_id, Money.Of(100m, "BRL"), _occurredAt);

    [Fact]
    public void Then_a_single_CreditPostedEvent_is_published() =>
        PublishedEvents.OfType<CreditPostedEvent>().ShouldHaveSingleItem();

    [Fact]
    public void Then_the_event_carries_the_right_data()
    {
        var @event = PublishedEvents.OfType<CreditPostedEvent>().ShouldHaveSingleItem();

        @event.AggregateId.ShouldBe(_id);
        @event.Amount.ShouldBe(Money.Of(100m, "BRL"));
        @event.OccurredAt.ShouldBe(_occurredAt);
    }

    [Fact]
    public void Then_the_aggregate_state_is_applied()
    {
        AggregateRoot.Id.ShouldBe(_id);
        AggregateRoot.Type.ShouldBe(EntryType.Credit);
        AggregateRoot.Amount.ShouldBe(Money.Of(100m, "BRL"));
        AggregateRoot.Version.ShouldBe(1);
    }
}

public class When_replaying_a_credit : AggregateTestFixture<Entry>
{
    private readonly Guid _id = Guid.NewGuid();

    protected override IEnumerable<IDomainEvent> Given() =>
    [
        new CreditPostedEvent(_id, Money.Of(100m, "BRL"), DateTime.UtcNow),
    ];

    [Fact]
    public void Then_the_state_is_reconstructed()
    {
        AggregateRoot.Id.ShouldBe(_id);
        AggregateRoot.Type.ShouldBe(EntryType.Credit);
        AggregateRoot.Amount.ShouldBe(Money.Of(100m, "BRL"));
        AggregateRoot.Version.ShouldBe(1);
    }

    [Fact]
    public void Then_replay_does_not_produce_uncommitted_events() =>
        PublishedEvents.ShouldBeEmpty();
}
