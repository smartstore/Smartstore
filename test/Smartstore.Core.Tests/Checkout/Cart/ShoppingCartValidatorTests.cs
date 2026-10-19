using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Identity;
using Smartstore.Events;

namespace Smartstore.Core.Tests.Checkout.Cart;

[TestFixture]
public class ShoppingCartValidatorTests : ServiceTestBase
{
    [TestCase(false, true, true, 0)]
    [TestCase(true, false, false, 1)]
    public async Task Can_validate_checkout_attributes(
        bool isActive,
        bool isSelected,
        bool expectedResult,
        int expectedWarningCount)
    {
        var customer = new Customer { Id = 1 };
        if (isSelected)
        {
            var selection = new CheckoutAttributeSelection(null);
            selection.AddAttributeValue(1, "Value");
            customer.GenericAttributes.CheckoutAttributes = selection;
        }

        var activeAttributes = new List<CheckoutAttribute>();
        if (isActive)
        {
            activeAttributes.Add(new CheckoutAttribute
            {
                Id = 1,
                Name = "Required attribute",
                IsRequired = true,
                IsActive = true
            });
        }

        var cart = new ShoppingCart(customer, 1, []);
        var materializer = new Mock<ICheckoutAttributeMaterializer>();
        materializer
            .Setup(x => x.GetCheckoutAttributesAsync(cart, cart.StoreId, true))
            .ReturnsAsync(activeAttributes);

        var warnings = new List<string>();
        var validator = CreateValidator(materializer.Object);
        var isValid = await validator.ValidateCartAsync(cart, warnings, true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(isValid, Is.EqualTo(expectedResult));
            Assert.That(warnings, Has.Count.EqualTo(expectedWarningCount));
            Assert.That(customer.GenericAttributes.CheckoutAttributes.HasAttributes, Is.False);
        }
    }

    private static ShoppingCartValidator CreateValidator(ICheckoutAttributeMaterializer materializer)
    {
        return new ShoppingCartValidator(
            null,
            null,
            null,
            null,
            null,
            new ShoppingCartSettings(),
            null,
            null,
            null,
            null,
            materializer,
            null,
            null,
            NullEventPublisher.Instance);
    }
}