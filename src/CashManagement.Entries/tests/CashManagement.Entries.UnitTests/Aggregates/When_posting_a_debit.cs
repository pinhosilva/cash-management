using CashManagement.Entries.Domain.Aggregates;
using CashManagement.Entries.Domain.Events;
using CashManagement.Entries.Domain.ValueObjects;
using CashManagement.Entries.UnitTests.Fixtures;
using Shouldly;
using Xunit;

namespace CashManagement.Entries.UnitTests.Aggregates;

public class When_posting_a_debit : AggregateTestFixture<Entry>
{
    private readonly Guid _id = Guid.NewGuid();
    private readonly DateTime _occurredAt = DateTime.UtcNow;

    protected override Entry When() => Entry.PostDebit(_id, Money.Of(100m, "BRL"), _occurredAt);

    [Fact]
    public void Then_a_single_DebitPostedEvent_is_published() =>
        PublishedEvents.OfType<DebitPostedEvent>().ShouldHaveSingleItem();

    [Fact]
    public void Then_the_event_carries_the_right_data()
    {
        var @event = PublishedEvents.OfType<DebitPostedEvent>().ShouldHaveSingleItem();

        @event.AggregateId.ShouldBe(_id);
        @event.Amount.ShouldBe(Money.Of(100m, "BRL"));
        @event.OccurredAt.ShouldBe(_occurredAt);
    }

    [Fact]
    public void Then_the_aggregate_state_is_applied()
    {
        AggregateRoot.Id.ShouldBe(_id);
        AggregateRoot.Type.ShouldBe(EntryType.Debit);
        AggregateRoot.Amount.ShouldBe(Money.Of(100m, "BRL"));
        AggregateRoot.Version.ShouldBe(1);
    }
}
