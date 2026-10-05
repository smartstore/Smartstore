using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Catalog.Pricing;

[TestFixture]
public class TierPriceExtensionTests
{
    [Test]
    public void Can_remove_duplicate_tierprice_quantities()
    {
        var tierPrices = new List<TierPrice>
        {
            new()
            {
                // Will be removed
                Id = 1,
                Price = 150,
                Quantity = 1
            },
            new()
            {
                // Will stay
                Id = 2,
                Price = 100,
                Quantity = 1
            },
            new()
            {
                // Will stay
                Id = 3,
                Price = 200,
                Quantity = 3
            },
            new()
            {
                // Will stay
                Id = 4,
                Price = 250,
                Quantity = 4
            },
            new()
            {
                // Will be removed
                Id = 5,
                Price = 300,
                Quantity = 4
            },
            new()
            {
                // Will stay
                Id = 6,
                Price = 350,
                Quantity = 5
            }
        };

        tierPrices.RemoveDuplicatedQuantities();

        tierPrices.FirstOrDefault(x => x.Id == 1).ShouldBeNull();
        tierPrices.FirstOrDefault(x => x.Id == 2).ShouldNotBeNull();
        tierPrices.FirstOrDefault(x => x.Id == 3).ShouldNotBeNull();
        tierPrices.FirstOrDefault(x => x.Id == 4).ShouldNotBeNull();
        tierPrices.FirstOrDefault(x => x.Id == 5).ShouldBeNull();
        tierPrices.FirstOrDefault(x => x.Id == 6).ShouldNotBeNull();
    }
}