using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Events;

namespace Smartstore.Core.Tests.Checkout.Cart;

[TestFixture]
public class ShoppingCartValidatorTests : ServiceTestBase
{
    [TestCase(false, false, true, false, true, false)]
    [TestCase(true, false, true, false, true, true)]
    [TestCase(true, true, false, false, true, false)]
    [TestCase(true, true, true, false, true, true)]
    [TestCase(true, false, true, true, false, false)]
    [TestCase(true, false, true, true, true, true)]
    public async Task Can_validate_required_product(
        bool published,
        bool subjectToAcl,
        bool aclAuthorized,
        bool limitedToStores,
        bool storeAuthorized,
        bool expectedResult)
    {
        var aclService = new Mock<IAclService>();
        aclService
            .Setup(x => x.AuthorizeAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IEnumerable<CustomerRole>>()))
            .ReturnsAsync(aclAuthorized);

        var storeMappingService = new Mock<IStoreMappingService>();
        storeMappingService
            .Setup(x => x.AuthorizeAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(storeAuthorized);

        var requiredProduct = new Product
        {
            Id = 1,
            Published = published,
            SubjectToAcl = subjectToAcl,
            LimitedToStores = limitedToStores
        };
        var product = new Product
        {
            Id = 2,
            Published = true,
            RequireOtherProducts = true,
            RequiredProductIds = requiredProduct.Id.ToString()
        };
        var item = new ShoppingCartItem
        {
            Customer = new Customer { Id = 1 },
            Product = requiredProduct,
            ProductId = requiredProduct.Id,
            StoreId = 1
        };

        var warnings = new List<string>();
        var validator = CreateValidator(aclService: aclService.Object, storeMappingService: storeMappingService.Object);
        var isValid = await validator.ValidateRequiredProductsAsync(product, [new OrganizedShoppingCartItem(item, true)], warnings);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(isValid, Is.EqualTo(expectedResult));
            Assert.That(warnings, Has.Count.EqualTo(expectedResult ? 0 : 1));
        }
    }

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

    private static ShoppingCartValidator CreateValidator(
        ICheckoutAttributeMaterializer materializer = null,
        IAclService aclService = null,
        IStoreMappingService storeMappingService = null)
    {
        return new ShoppingCartValidator(
            null,
            aclService,
            null,
            null,
            null,
            new ShoppingCartSettings(),
            null,
            storeMappingService,
            null,
            null,
            materializer,
            null,
            null,
            NullEventPublisher.Instance);
    }
}