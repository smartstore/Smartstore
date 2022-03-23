using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Smartstore.Caching;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Identity;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Shipping
{
    [TestFixture]
    public class ShippingServiceTests : ServiceTestBase
    {
        ShippingSettings _shippingSettings;
        IShippingService _shippingService;
        IProductAttributeMaterializer _productAttributeMaterializer;
        IRequestCache _requestCache;

        [OneTimeSetUp]
        public new void SetUp()
        {
            _shippingSettings = new ShippingSettings
            {
                ActiveShippingRateComputationMethodSystemNames = new List<string>
                {
                    "FixedRateTestShippingRateComputationMethod"
                }
            };

            _requestCache = new NullRequestCache();
            _productAttributeMaterializer = new ProductAttributeMaterializer(null, null, _requestCache, null, null, new Lazy<CatalogSettings>(), null);

            DbContext.ShippingMethods.Add(new ShippingMethod { Name = "1" });
            DbContext.ShippingMethods.Add(new ShippingMethod { Name = "2" });
            DbContext.ShippingMethods.Add(new ShippingMethod { Name = "3" });
            DbContext.ShippingMethods.Add(new ShippingMethod { Name = "4" });
            DbContext.SaveChanges();

            _shippingService = new ShippingService(
                _productAttributeMaterializer,
                null,
                null,
                _shippingSettings,
                ProviderManager,
                null,
                null,
                DbContext);
        }

        [Test]
        public async Task Can_load_shippingRateComputationMethods()
        {
            var srcm = await _shippingService.GetAllShippingMethodsAsync();
            srcm.ShouldNotBeNull();
            (srcm.Count > 0).ShouldBeTrue();
        }

        [Test]
        public void Can_load_shippingRateComputationMethod_by_systemKeyword()
        {
            var srcm = _shippingService.LoadActiveShippingRateComputationMethods(systemName: "FixedRateTestShippingRateComputationMethod").FirstOrDefault();
            srcm.Value.ShouldNotBeNull();
        }

        [Test]
        public void Can_load_active_shippingRateComputationMethods()
        {
            var srcm = _shippingService.LoadActiveShippingRateComputationMethods();
            srcm.ShouldNotBeNull();
            srcm.Any().ShouldBeTrue();
        }

        [Test]
        public async Task Can_get_shoppingCartItem_totalWeight_without_attributes()
        {
            var sci = new ShoppingCartItem
            {
                RawAttributes = "",
                Quantity = 3,
                Product = new Product
                {
                    Weight = 1.5M,
                    Height = 2.5M,
                    Length = 3.5M,
                    Width = 4.5M
                }
            };

            var item = new OrganizedShoppingCartItem(sci);

            (await _shippingService.GetCartItemWeightAsync(item)).ShouldEqual(4.5M);
        }

        [Test]
        public async Task Can_get_shoppingCart_totalWeight_without_attributes()
        {
            var sci1 = new ShoppingCartItem
            {
                RawAttributes = string.Empty,
                Quantity = 3,
                Product = new Product
                {
                    Weight = 1.5M,
                    Height = 2.5M,
                    Length = 3.5M,
                    Width = 4.5M
                }
            };
            var sci2 = new ShoppingCartItem
            {
                RawAttributes = string.Empty,
                Quantity = 4,
                Product = new Product
                {
                    Weight = 11.5M,
                    Height = 12.5M,
                    Length = 13.5M,
                    Width = 14.5M
                }
            };

            var items = new List<OrganizedShoppingCartItem>
            {
                new OrganizedShoppingCartItem(sci1),
                new OrganizedShoppingCartItem(sci2)
            };

            var customer = new Customer
            {
                Id = 1,
            };

            var cart = new ShoppingCart(customer, 0, items);

            (await _shippingService.GetCartTotalWeightAsync(cart)).ShouldEqual(50.5M);
        }
    }
}
