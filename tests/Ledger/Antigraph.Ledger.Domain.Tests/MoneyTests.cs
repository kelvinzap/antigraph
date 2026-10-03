using Antigraph.Ledger.Domain.Models.Entities;


namespace Antigraph.Ledger.Domain.Tests;

public class MoneyTests
{
    // Test for the addition operator of the Money class
    [Fact]
    public void Money_TwoValuesAdded_ReturnsSum()
    {
        // Arrange
        var money1 = new Money(1000, Currency.GBP);
        var money2 = new Money(1500, Currency.GBP);

        // Act
        var result = money1 + money2;

        // Assert
        Assert.Equal(new Money(2500, Currency.GBP), result);
    }
    
     [Fact]
    public void Money_TwoDifferentCurrencyValuesAdded_ThrowsError()
    {
        // Arrange
        var money1 = new Money(1000, Currency.GBP);
        var money2 = new Money(1500, Currency.USD);

    

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => money1 + money2);
    }
}