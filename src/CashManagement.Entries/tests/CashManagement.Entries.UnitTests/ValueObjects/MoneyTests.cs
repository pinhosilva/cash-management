using CashManagement.Entries.Domain.ValueObjects;
using Shouldly;
using Xunit;

namespace CashManagement.Entries.UnitTests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Of_creates_money_with_amount_and_currency()
    {
        var money = Money.Of(100m, "BRL");

        money.Amount.ShouldBe(100m);
        money.Currency.ShouldBe("BRL");
    }

    [Fact]
    public void Two_money_with_same_amount_and_currency_are_equal()
    {
        Money.Of(10m, "BRL").ShouldBe(Money.Of(10m, "BRL"));
        (Money.Of(10m, "BRL") == Money.Of(10m, "BRL")).ShouldBeTrue();
    }

    [Fact]
    public void Money_with_different_amount_are_not_equal()
    {
        Money.Of(10m, "BRL").ShouldNotBe(Money.Of(20m, "BRL"));
    }
}
