using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Checkout.Orders
{
    [TestFixture]
    public class CheckoutAttributeExtensionsTests : ServiceTest
    {
        [Test]
        public void Can_remove_shippable_attributes()
        {
            var attributes = new List<CheckoutAttribute>
            {
                new CheckoutAttribute()
                {
                    Id = 1,
                    Name = "Attribute 1",
                    ShippableProductRequired = false,
                },
                new CheckoutAttribute()
                {
                    Id = 2,
                    Name = "Attribute 2",
                    ShippableProductRequired = true,
                },
                new CheckoutAttribute()
                {
                    Id = 3,
                    Name = "Attribute 3",
                    ShippableProductRequired = false,
                },
                new CheckoutAttribute()
                {
                    Id = 4,
                    Name = "Attribute 4",
                    ShippableProductRequired = true,
                }
            };

            var filtered = attributes.Where(x => !x.ShippableProductRequired).ToList();
            filtered.Count.ShouldEqual(2);
            filtered[0].Id.ShouldEqual(1);
            filtered[1].Id.ShouldEqual(3);
        }
    }
}