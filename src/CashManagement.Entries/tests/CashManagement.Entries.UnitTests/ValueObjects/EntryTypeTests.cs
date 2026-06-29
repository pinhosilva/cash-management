using CashManagement.Entries.Domain.ValueObjects;
using Shouldly;
using Xunit;

namespace CashManagement.Entries.UnitTests.ValueObjects;

public class EntryTypeTests
{
    [Fact]
    public void Credit_is_defined()
    {
        EntryType.Credit.ShouldNotBeNull();
        EntryType.Credit.Name.ShouldBe("Credit");
    }

    [Fact]
    public void Credit_references_are_equal_by_value()
    {
        var a = EntryType.Credit;
        var b = EntryType.Credit;

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
    }

    [Fact]
    public void Debit_is_defined()
    {
        EntryType.Debit.ShouldNotBeNull();
        EntryType.Debit.Name.ShouldBe("Debit");
    }

    [Fact]
    public void Credit_and_debit_are_distinct()
    {
        EntryType.Credit.ShouldNotBe(EntryType.Debit);
        (EntryType.Credit == EntryType.Debit).ShouldBeFalse();
    }
}
