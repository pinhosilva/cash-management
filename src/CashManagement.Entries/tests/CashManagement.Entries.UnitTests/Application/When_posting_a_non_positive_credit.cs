using CashManagement.Entries.Application.Features.PostCredit;
using CashManagement.Entries.Application.Interfaces;
using CashManagement.Entries.Domain.Aggregates;
using CashManagement.Entries.Domain.Persistence;
using CashManagement.Entries.Domain.SeedWork;
using CashManagement.Entries.UnitTests.Fixtures;
using Moq;
using Shouldly;
using Xunit;

namespace CashManagement.Entries.UnitTests.Application;

public class When_posting_a_non_positive_credit
    : CommandTestFixture<PostCreditCommand, PostCreditCommandHandler, Entry>
{
    protected override PostCreditCommand When() => new(0m, DateTime.UtcNow);

    protected override PostCreditCommandHandler CreateHandler(IRepository repository, IIdGenerator idGenerator) =>
        new(repository, idGenerator, new PostCreditCommandValidator());

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
    public void Then_nothing_is_added() =>
        Repository.Verify(r => r.Add(It.IsAny<AggregateRoot>()), Times.Never);
}
