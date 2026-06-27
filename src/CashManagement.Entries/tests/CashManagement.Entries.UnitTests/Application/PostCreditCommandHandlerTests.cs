using CashManagement.Entries.Application.Features.PostCredit;
using CashManagement.Entries.Application.Interfaces;
using CashManagement.Entries.Domain.Aggregates;
using CashManagement.Entries.Domain.Events;
using CashManagement.Entries.Domain.Persistence;
using CashManagement.Entries.Domain.SeedWork;
using CashManagement.Entries.UnitTests.Fixtures;
using Moq;
using Shouldly;
using Xunit;

namespace CashManagement.Entries.UnitTests.Application;

public class When_posting_a_valid_credit
    : CommandTestFixture<PostCreditCommand, PostCreditCommandHandler, Entry>
{
    protected override PostCreditCommand When() => new(100m, DateTime.UtcNow);

    protected override PostCreditCommandHandler CreateHandler(IEventStore eventStore, IIdGenerator idGenerator) =>
        new(eventStore, idGenerator, new PostCreditCommandValidator());

    [Fact]
    public void Then_a_single_CreditPostedEvent_is_published() =>
        PublishedEvents.OfType<CreditPostedEvent>().ShouldHaveSingleItem();

    [Fact]
    public void Then_the_result_is_success_with_the_generated_id()
    {
        Result.IsSuccess.ShouldBeTrue();
        Result.Value.ShouldBe(GeneratedId);
    }

    [Fact]
    public void Then_the_events_are_appended_once() =>
        EventStore.Verify(s => s.Append(It.IsAny<Entry>()), Times.Once);
}

public class When_posting_a_non_positive_credit
    : CommandTestFixture<PostCreditCommand, PostCreditCommandHandler, Entry>
{
    protected override PostCreditCommand When() => new(0m, DateTime.UtcNow);

    protected override PostCreditCommandHandler CreateHandler(IEventStore eventStore, IIdGenerator idGenerator) =>
        new(eventStore, idGenerator, new PostCreditCommandValidator());

    [Fact]
    public void Then_the_result_is_a_validation_failure()
    {
        Result.IsFailure.ShouldBeTrue();
        Result.Error!.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public void Then_no_event_is_published() =>
        PublishedEvents.ShouldBeEmpty();

    [Fact]
    public void Then_nothing_is_appended() =>
        EventStore.Verify(s => s.Append(It.IsAny<AggregateRoot>()), Times.Never);
}
