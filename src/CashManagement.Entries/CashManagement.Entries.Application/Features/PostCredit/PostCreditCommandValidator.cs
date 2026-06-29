using CashManagement.Entries.Domain.SeedWork;

namespace CashManagement.Entries.Application.Features.PostCredit;

/// <summary>
/// Valida o <see cref="PostCreditCommand"/> antes de tocar o domínio. Falha
/// vira <see cref="Result"/> com <see cref="ErrorType.Validation"/> — sem exceção.
/// </summary>
public sealed class PostCreditCommandValidator
{
    public Result Validate(PostCreditCommand command)
    {
        if (command.Amount <= 0)
        {
            return Result.Fail(new Error("VALIDATION_FAILED", "Amount must be positive.", ErrorType.Validation));
        }

        if (command.OccurredAt == default)
        {
            return Result.Fail(new Error("VALIDATION_FAILED", "OccurredAt must be a valid date.", ErrorType.Validation));
        }

        return Result.Ok();
    }
}
