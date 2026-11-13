using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Smartstore.Caching;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
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
        _validatorMock
            .Setup(x => x.ValidateRequiredProductsAsync(It.IsAny<Product>(), It.IsAny<IEnumerable<OrganizedShoppingCartItem>>(), It.IsAny<IList<string>>()))
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

    [Test]
    public async Task Can_add_restricted_required_product()
    {
        var service = CreateAddToCartService();
        var requiredProduct = new Product
        {
            Name = "Required product",
            Published = true,
            Visibility = ProductVisibility.ProductPage,
            ManageInventoryMethod = ManageInventoryMethod.DontManageStock
        };
        var product = new Product
        {
            Name = "Main product",
            Published = true,
            RequireOtherProducts = true,
            AutomaticallyAddRequiredProducts = true
        };

        _testProducts.AddRange([product, requiredProduct]);
        DbContext.Products.AddRange(product, requiredProduct);
        await DbContext.SaveChangesAsync();

        product.RequiredProductIds = requiredProduct.Id.ToString();
        var context = new AddToCartContext
        {
            Customer = _customer,
            Product = product,
            StoreId = 1,
            Quantity = 1,
            AutomaticallyAddRequiredProducts = true
        };

        var result = await service.AddToCartAsync(context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(context.Warnings, Is.Empty);
        }

        Mock.Get(service).Verify(x => x.AddItemToCartAsync(
            It.Is<AddToCartContext>(ctx => ctx.RequiredItems.Count == 1 && ctx.RequiredItems[0].Product.Id == requiredProduct.Id)),
            Times.Once);
    }

    [Test]
    public async Task Cannot_add_inaccessible_required_product()
    {
        var service = CreateAddToCartService();
        var requiredProducts = new[]
        {
            new Product { Name = "Accessible required product", Published = true },
            new Product { Name = "Inaccessible required product", Published = true }
        };
        var product = new Product
        {
            Name = "Main product",
            Published = true,
            RequireOtherProducts = true,
            AutomaticallyAddRequiredProducts = true
        };

        _testProducts.Add(product);
        _testProducts.AddRange(requiredProducts);
        DbContext.Products.Add(product);
        DbContext.Products.AddRange(requiredProducts);
        await DbContext.SaveChangesAsync();

        product.RequiredProductIds = string.Join(',', requiredProducts.Select(x => x.Id));
        _validatorMock
            .Setup(x => x.ValidateRequiredProductsAsync(
                product,
                It.IsAny<IEnumerable<OrganizedShoppingCartItem>>(),
                It.IsAny<IList<string>>()))
            .Callback<Product, IEnumerable<OrganizedShoppingCartItem>, IList<string>>((_, _, warnings) => warnings.Add("Product is inaccessible."))
            .ReturnsAsync(false);

        var context = new AddToCartContext
        {
            Customer = _customer,
            Product = product,
            StoreId = 1,
            Quantity = 1,
            AutomaticallyAddRequiredProducts = true
        };

        var result = await service.AddToCartAsync(context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            Assert.That(context.Warnings, Has.Count.EqualTo(1));
        }

        Mock.Get(service).Verify(x => x.AddItemToCartAsync(It.IsAny<AddToCartContext>()), Times.Never);
    }

    [Test]
    public async Task Cannot_add_required_product_with_invalid_main_product()
    {
        var service = CreateAddToCartService();
        var requiredProduct = new Product { Name = "Required product", Published = true };
        var product = new Product
        {
            Name = "Invalid main product",
            Published = true,
            RequireOtherProducts = true,
            AutomaticallyAddRequiredProducts = true
        };

        _testProducts.AddRange([product, requiredProduct]);
        DbContext.Products.AddRange(product, requiredProduct);
        await DbContext.SaveChangesAsync();

        product.RequiredProductIds = requiredProduct.Id.ToString();
        _validatorMock
            .Setup(x => x.ValidateAddToCartItemAsync(
                It.IsAny<AddToCartContext>(),
                It.Is<ShoppingCartItem>(item => item.ProductId == product.Id),
                It.IsAny<IEnumerable<OrganizedShoppingCartItem>>()))
            .Callback<AddToCartContext, ShoppingCartItem, IEnumerable<OrganizedShoppingCartItem>>(
                (ctx, _, _) => ctx.Warnings.Add("Main product is invalid."))
            .ReturnsAsync(false);

        var context = new AddToCartContext
        {
            Customer = _customer,
            Product = product,
            StoreId = 1,
            Quantity = 1,
            AutomaticallyAddRequiredProducts = true
        };

        var result = await service.AddToCartAsync(context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            Assert.That(context.Warnings, Has.Count.EqualTo(1));
            Assert.That(context.RequiredItems, Has.Count.EqualTo(1));
        }

        Mock.Get(service).Verify(x => x.AddItemToCartAsync(It.IsAny<AddToCartContext>()), Times.Never);
    }

    [Test]
    public async Task Can_copy_required_products()
    {
        var service = CreateAddToCartService();
        var contexts = new List<AddToCartContext>();
        var requiredItems = new List<ShoppingCartItem>();
        var bundleItem = new ProductBundleItem { Product = new Product { Name = "Bundle item" } };
        var context = new AddToCartContext
        {
            Customer = _customer,
            Product = new Product { Name = "Bundle" },
            StoreId = 1,
            Quantity = 1,
            CustomerEnteredPrice = new(0, new Currency { CurrencyCode = "EUR" }),
            AutomaticallyAddRequiredProducts = true,
            RequiredItems = requiredItems,
            ChildItems =
            [
                new ShoppingCartItem
                {
                    BundleItem = bundleItem,
                    Product = bundleItem.Product,
                    Quantity = 1
                }
            ]
        };

        Mock.Get(service)
            .Setup(x => x.AddToCartAsync(It.IsAny<AddToCartContext>()))
            .Callback<AddToCartContext>(contexts.Add)
            .ReturnsAsync(true);

        var result = await service.CopyAsync(context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(contexts, Has.Count.EqualTo(2));
            Assert.That(contexts[0].AutomaticallyAddRequiredProducts, Is.True);
            Assert.That(contexts[0].RequiredItems, Is.SameAs(requiredItems));
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