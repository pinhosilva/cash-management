using CashManagement.Entries.Application.Features.PostDebit;
using CashManagement.Entries.Application.Interfaces;
using CashManagement.Entries.Domain.Aggregates;
using CashManagement.Entries.Domain.Events;
using CashManagement.Entries.Domain.Persistence;
using CashManagement.Entries.UnitTests.Fixtures;
using Moq;
using Shouldly;
using Xunit;

namespace CashManagement.Entries.UnitTests.Application;

public class When_posting_a_valid_debit
    : CommandTestFixture<PostDebitCommand, PostDebitCommandHandler, Entry>
{
    protected override PostDebitCommand When() => new(100m, DateTime.UtcNow);

    protected override PostDebitCommandHandler CreateHandler(IRepository repository, IIdGenerator idGenerator) =>
        new(repository, idGenerator, new PostDebitCommandValidator());

    [Fact]
    public void Then_a_single_DebitPostedEvent_is_published() =>
        PublishedEvents.OfType<DebitPostedEvent>().ShouldHaveSingleItem();

    [Fact]
    public void Then_the_result_is_success_with_the_generated_id()
    {
        Result.IsSuccess.ShouldBeTrue();
        Result.Value.ShouldBe(GeneratedId);
    }

    [Fact]
    public void Then_the_aggregate_is_added_once() =>
        Repository.Verify(r => r.Add(It.IsAny<Entry>()), Times.Once);
}
