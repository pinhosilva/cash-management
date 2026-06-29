using CashManagement.Entries.Domain.SeedWork;
using Shouldly;
using Xunit;

namespace CashManagement.Entries.UnitTests.SeedWork;

public class ResultTests
{
    [Fact]
    public void Ok_is_success_without_error()
    {
        var result = Result.Ok();

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Error.ShouldBeNull();
    }

    [Fact]
    public void Fail_carries_error_and_is_not_success()
    {
        var error = new Error("VALIDATION_FAILED", "Invalid amount.", ErrorType.Validation);

        var result = Result.Fail(error);

        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(error);
    }

    [Fact]
    public void Ok_with_value_exposes_value()
    {
        var id = Guid.NewGuid();

        Result<Guid> result = Result.Ok(id);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(id);
    }

    [Fact]
    public void Accessing_value_of_failed_result_throws()
    {
        Result<Guid> result = Result.Fail<Guid>(new Error("CONFLICT", "Boom.", ErrorType.Conflict));

        result.IsFailure.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() => _ = result.Value);
    }
}
