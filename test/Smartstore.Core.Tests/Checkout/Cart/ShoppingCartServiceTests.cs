using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Smartstore.Caching;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Identity;
using Smartstore.Events;

namespace Smartstore.Core.Tests.Checkout.Cart;

[TestFixture]
public class ShoppingCartServiceTests : ServiceTestBase
{
    Mock<IShoppingCartValidator> _validatorMock;
    Customer _customer;
    List<Product> _testProducts;
    List<ProductBundleItem> _testBundleItems;

    [SetUp]
    public new void SetUp()
    {
        _customer = new Customer { Id = 1 };
        _testProducts = [];
        _testBundleItems = [];

        _validatorMock = new Mock<IShoppingCartValidator>();
        _validatorMock
            .Setup(x => x.ValidateAccessPermissionsAsync(It.IsAny<Customer>(), It.IsAny<ShoppingCartType>(), It.IsAny<IList<string>>()))
            .ReturnsAsync(true);
        _validatorMock
            .Setup(x => x.ValidateItemsMaximumCartQuantity(It.IsAny<ShoppingCartType>(), It.IsAny<int>(), It.IsAny<IList<string>>()))
            .Returns(true);
        _validatorMock
            .Setup(x => x.ValidateAddToCartItemAsync(It.IsAny<AddToCartContext>(), It.IsAny<ShoppingCartItem>(), It.IsAny<IEnumerable<OrganizedShoppingCartItem>>()))
            .ReturnsAsync(true);
    }

    [TearDown]
    public async Task TearDownTest()
    {
        DbContext.ProductBundleItem.RemoveRange(_testBundleItems);
        DbContext.Products.RemoveRange(_testProducts);
        await DbContext.SaveChangesAsync();
    }

    [TestCase(false, true, 0)]
    [TestCase(true, false, 1)]
    public async Task Can_add_only_active_bundle_items(bool published, bool visible, int expectedChildItemCount)
    {
        var service = CreateAddToCartService();
        var bundleProduct = await CreateBundle(published, visible);
        var context = new AddToCartContext
        {
            Customer = _customer,
            Product = bundleProduct,
            StoreId = 1,
            Quantity = 1,
            AutomaticallyAddBundleProducts = true
        };

        var result = await service.AddToCartAsync(context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(context.Warnings, Is.Empty);
            Assert.That(context.ChildItems, Has.Count.EqualTo(expectedChildItemCount));
        }
    }

    private ShoppingCartService CreateAddToCartService()
    {
        var service = new Mock<ShoppingCartService>(
            DbContext,
            null,
            null,
            NullRequestCache.Instance,
            NullEventPublisher.Instance,
            _validatorMock.Object,
            null,
            null,
            null,
            new Lazy<ICheckoutFactory>(() => null),
            Mock.Of<ICurrencyService>(),
            new RewardPointsSettings(),
            new ShoppingCartSettings())
        {
            CallBase = true
        };

        service
            .Setup(x => x.GetCartAsync(It.IsAny<Customer>(), It.IsAny<ShoppingCartType>(), It.IsAny<int>(), It.IsAny<bool?>()))
            .ReturnsAsync((Customer customer, ShoppingCartType cartType, int storeId, bool? _) =>
                new ShoppingCart(customer, storeId, []) { CartType = cartType });
        service
            .Setup(x => x.AddItemToCartAsync(It.IsAny<AddToCartContext>()))
            .Returns(Task.CompletedTask);

        return service.Object;
    }

    private async Task<Product> CreateBundle(bool published, bool visible)
    {
        var bundleProduct = new Product
        {
            Name = "Bundle",
            ProductType = ProductType.BundledProduct,
            Published = true
        };

        var bundleItemProduct = new Product
        {
            Name = "Bundle item",
            ProductType = ProductType.SimpleProduct,
            Published = true
        };

        var bundleItem = new ProductBundleItem
        {
            BundleProduct = bundleProduct,
            Product = bundleItemProduct,
            Quantity = 1,
            Published = published,
            Visible = visible
        };

        _testProducts.AddRange([bundleProduct, bundleItemProduct]);
        _testBundleItems.Add(bundleItem);

        DbContext.ProductBundleItem.Add(bundleItem);
        await DbContext.SaveChangesAsync();

        return bundleProduct;
    }
}