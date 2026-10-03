using System;

using Antigraph.Ledger.Domain.Models.Entities;

namespace Antigraph.Ledger.Domain.Tests;

public class JournalTests
{
    [Fact]
    public void Journal_TwoBalancedPostings_JournalCreatedSuccessfully()
    {
        // Arrange    
        string paymentId = "payment123";    
        Guid AccountId = Guid.NewGuid();
        var entry1 = new Posting { Amount = new Money(1000, Currency.GBP), Direction = Direction.CR,  AccountId = AccountId };
        var entry2 = new Posting{ Amount = new Money(1000, Currency.GBP), Direction = Direction.DR,  AccountId = AccountId };

        //Act
        var journal = new Journal(new List<Posting> { entry1, entry2 }, paymentId);

        // Assert
        Assert.NotNull(journal);
    }
    
        [Fact]
    public void Journal_TwoUnbalancedPostings_JournalNotCreated()
    {
        // Arrange    
        string paymentId = "payment123";    
        Guid AccountId = Guid.NewGuid();
        var entry1 = new Posting { Amount = new Money(1000, Currency.GBP), Direction = Direction.CR,  AccountId = AccountId };
        var entry2 = new Posting{ Amount = new Money(2000, Currency.GBP), Direction = Direction.DR,  AccountId = AccountId };
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Journal(new List<Posting> { entry1, entry2 }, paymentId));
    }
    
    [Fact]
     public void Journal_ThreeBalancedPostings_JournalCreatedSuccessfully()
    {
        // Arrange    
        string paymentId = "payment123";    
        Guid AccountId = Guid.NewGuid();
        var entry1 = new Posting { Amount = new Money(10050, Currency.GBP), Direction = Direction.DR,  AccountId = AccountId };
        var entry2 = new Posting{ Amount = new Money(10000, Currency.GBP), Direction = Direction.CR,  AccountId = AccountId };
        var entry3 = new Posting{ Amount = new Money(50, Currency.GBP), Direction = Direction.CR,  AccountId = AccountId };

        //Act
        var journal = new Journal(new List<Posting> { entry1, entry2, entry3 }, paymentId);

        // Assert
        Assert.NotNull(journal);
    }
    
    [Fact]
     public void Journal_ThreeUnbalancedPostings_JournalNotCreated()
    {
        // Arrange    
        string paymentId = "payment123";    
        Guid AccountId = Guid.NewGuid();
        var entry1 = new Posting { Amount = new Money(10050, Currency.GBP), Direction = Direction.DR, AccountId = AccountId };
        var entry2 = new Posting{ Amount = new Money(10000, Currency.GBP), Direction = Direction.CR, AccountId = AccountId };
        var entry3 = new Posting{ Amount = new Money(650, Currency.GBP), Direction = Direction.CR, AccountId = AccountId };

        //Act & Assert
        Assert.Throws<ArgumentException>(() => new Journal(new List<Posting> { entry1, entry2, entry3 }, paymentId));        
    }
    
      [Fact]
     public void Journal_SinglePosting_JournalNotCreated()
    {
        // Arrange    
        string paymentId = "payment123";    
        Guid AccountId = Guid.NewGuid();
        var entry1 = new Posting { Amount = new Money(10050, Currency.GBP), Direction = Direction.DR, AccountId = AccountId };
       

        //Act & Assert
        Assert.Throws<ArgumentException>(() => new Journal(new List<Posting> { entry1 }, paymentId));        
    }
    
    
    [Fact]
     public void Journal_AllSameDirectionPostings_JournalNotCreated()
    {
        // Arrange    
        string paymentId = "payment123";    
        Guid AccountId = Guid.NewGuid();
        var entry1 = new Posting { Amount = new Money(10050, Currency.GBP), Direction = Direction.DR, AccountId = AccountId };
        var entry2 = new Posting{ Amount = new Money(10000, Currency.GBP), Direction = Direction.DR, AccountId = AccountId };
        var entry3 = new Posting{ Amount = new Money(50, Currency.GBP), Direction = Direction.DR, AccountId = AccountId };

        //Act & Assert
        Assert.Throws<ArgumentException>(() => new Journal(new List<Posting> { entry1, entry2, entry3 }, paymentId));        
    }
    
     [Fact]
     public void Journal_MixedCurrencyPostings_JournalNotCreated()
    {
        // Arrange    
        string paymentId = "payment123";    
        Guid AccountId = Guid.NewGuid();
        var entry1 = new Posting { Amount = new Money(10050, Currency.GBP), Direction = Direction.DR, AccountId = AccountId };
        var entry2 = new Posting{ Amount = new Money(10000, Currency.USD), Direction = Direction.CR, AccountId = AccountId };
        var entry3 = new Posting{ Amount = new Money(50, Currency.JPY), Direction = Direction.CR, AccountId = AccountId };

        //Act & Assert
        Assert.Throws<ArgumentException>(() => new Journal(new List<Posting> { entry1, entry2, entry3 }, paymentId));        
    }
    
     [Fact]
     public void Journal_EmptyPostingsList_JournalNotCreated()
    {       
        //Arrange Act & Assert
        Assert.Throws<ArgumentException>(() => new Journal(new List<Posting> { }, "payment123"));        
    }
}
