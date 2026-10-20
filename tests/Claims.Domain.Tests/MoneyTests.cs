using Claims.Domain;

namespace Claims.Domain.Tests;

public class MoneyTests
{
    [Fact]
    public void Create_normalises_currency_to_upper_case()
    {
        var money = Money.Create(10m, " egp ");

        Assert.Equal("EGP", money.Currency);
        Assert.Equal(10m, money.Amount);
    }

    [Fact]
    public void Create_rejects_negative_amount()
    {
        Assert.Throws<ArgumentException>(() => Money.Create(-1m, "Egp"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("US")]
    [InlineData("EgpX")]
    public void Create_rejects_invalid_currency(string? currency)
    {
        Assert.Throws<ArgumentException>(() => Money.Create(10m, currency!));
    }

    [Fact]
    public void Add_sums_amounts_in_same_currency()
    {
        var total = Money.Create(10m, "Egp").Add(Money.Create(5m, "Egp"));

        Assert.Equal(Money.Create(15m, "Egp"), total);
    }

    [Fact]
    public void Add_rejects_different_currencies()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Money.Create(10m, "Egp").Add(Money.Create(5m, "EUR")));
    }

    [Fact]
    public void Money_with_same_values_is_equal()
    {
        Assert.Equal(Money.Create(10m, "egp"), Money.Create(10m, "Egp"));
    }
}
