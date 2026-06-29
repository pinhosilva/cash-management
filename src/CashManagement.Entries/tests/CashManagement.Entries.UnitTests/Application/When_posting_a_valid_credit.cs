using CashManagement.Entries.Application.Features.PostCredit;
using CashManagement.Entries.Application.Interfaces;
using CashManagement.Entries.Domain.Aggregates;
using CashManagement.Entries.Domain.Events;
using CashManagement.Entries.Domain.Persistence;
using CashManagement.Entries.UnitTests.Fixtures;
using Moq;
using Shouldly;
using Xunit;

namespace CashManagement.Entries.UnitTests.Application;

public class When_posting_a_valid_credit
    : CommandTestFixture<PostCreditCommand, PostCreditCommandHandler, Entry>
{
    protected override PostCreditCommand When() => new(100m, DateTime.UtcNow);

    protected override PostCreditCommandHandler CreateHandler(IRepository repository, IIdGenerator idGenerator) =>
        new(repository, idGenerator, new PostCreditCommandValidator());

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
    public void Then_the_aggregate_is_added_once() =>
        Repository.Verify(r => r.Add(It.IsAny<Entry>()), Times.Once);
}
