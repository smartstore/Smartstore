using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Smartstore.Caching;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Rules;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Stores;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Checkout.Orders
{
    [TestFixture]
    public class OrderTotalCalculationServiceTests : ServiceTestBase
    {
        IWorkContext _workContext;
        IStoreContext _storeContext;
        ITaxService _taxService;
        IShippingService _shippingService;
        IDiscountService _discountService;
        IGiftCardService _giftCardService;
        ICurrencyService _currencyService;
        IRoundingHelper _roundingHelper;
        ILocalizationService _localizationService;
        TaxSettings _taxSettings;
        RewardPointsSettings _rewardPointsSettings;
        IProductService _productService;
        IPriceCalculationService _priceCalcService;
        IOrderCalculationService _orderCalcService;
        ShippingSettings _shippingSettings;
        PriceSettings _priceSettings;
        CurrencySettings _currencySettings;
        IPriceCalculatorFactory _priceCalculatorFactory;
        ITaxCalculator _taxCalculator;
        IProductAttributeMaterializer _productAttributeMaterializer;
        ICheckoutAttributeMaterializer _checkoutAttributeMaterializer;
        ViesTaxationHttpClient _client;

        IRequestCache _requestCache;
        ProductBatchContext _productBatchContext;
        Mock<IDiscountService> _discountServiceMock;

        Store _store;
        Currency _currency;
        Customer _customer;
        Language _language;

        [SetUp]
        public new void SetUp()
        {
            _store = new Store { Id = 1 };
            _currency = new Currency { Id = 1, RoundNumDecimals = 3 };
            _customer = new Customer { Id = 1 };
            _language = new Language { Id = 1 };

            var storeContextMock = new Mock<IStoreContext>();
            _storeContext = storeContextMock.Object;
            storeContextMock.Setup(x => x.CurrentStore).Returns(_store);

            var workContextMock = new Mock<IWorkContext>();
            _workContext = workContextMock.Object;
            workContextMock.Setup(x => x.WorkingCurrency).Returns(_currency);
            workContextMock.Setup(x => x.WorkingLanguage).Returns(_language);

            _requestCache = new NullRequestCache();

            var productServiceMock = new Mock<IProductService>();
            _productService = productServiceMock.Object;
            productServiceMock
                .Setup(x => x.CreateProductBatchContext(It.IsAny<IEnumerable<Product>>(), null, _customer, false, false))
                .Returns(new ProductBatchContext(new List<Product>(), DbContext, LifetimeScope, _store, _customer, false));

            _currencySettings = new CurrencySettings();
            _rewardPointsSettings = new RewardPointsSettings();
            _priceSettings = new PriceSettings();
            _taxSettings = new TaxSettings
            {
                ShippingPriceIncludesTax = false,
                ShippingIsTaxable = true,
                PaymentMethodAdditionalFeeIsTaxable = true,
                PricesIncludeTax = false,
                TaxDisplayType = TaxDisplayType.IncludingTax,
                DefaultTaxAddressId = 10
            };
            _shippingSettings = new ShippingSettings
            {
                ActiveShippingRateComputationMethodSystemNames =
                [
                    "FixedRateTestShippingRateComputationMethod"
                ]
            };

            var giftCardServiceMock = new Mock<IGiftCardService>();
            _giftCardService = giftCardServiceMock.Object;
            giftCardServiceMock
                .Setup(x => x.GetValidGiftCardsAsync(It.IsAny<int>(), It.IsAny<Customer>()))
                .ReturnsAsync([]);

            var productAttributeMaterializerMock = new Mock<IProductAttributeMaterializer>();
            _productAttributeMaterializer = productAttributeMaterializerMock.Object;

            var checkoutAttributeMaterializerMock = new Mock<ICheckoutAttributeMaterializer>();
            _checkoutAttributeMaterializer = checkoutAttributeMaterializerMock.Object;

            _discountServiceMock = new Mock<IDiscountService>();
            _discountService = _discountServiceMock.Object;

            _discountServiceMock
                .Setup(x => x.GetAllDiscountsAsync(DiscountType.AssignedToCategories, It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<Discount>());
            _discountServiceMock
                .Setup(x => x.GetAllDiscountsAsync(DiscountType.AssignedToManufacturers, It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<Discount>());

            var currencyServiceMock = new Mock<ICurrencyService>();
            _currencyService = currencyServiceMock.Object;
            currencyServiceMock.Setup(x => x.PrimaryCurrency).Returns(_currency);
            currencyServiceMock.Setup(x => x.PrimaryExchangeCurrency).Returns(_currency);

            _roundingHelper = new RoundingHelper(_workContext, _currencySettings);

            _client = new ViesTaxationHttpClient(new HttpClient());

            var priceLabelService = new Mock<IPriceLabelService>();

            var localizationServiceMock = new Mock<ILocalizationService>();
            _localizationService = localizationServiceMock.Object;

            // INFO: no mocking here to use real implementation.
            _taxService = new TaxService(DbContext, null, ProviderManager, _workContext, _roundingHelper, _localizationService, _taxSettings, _client);
            _taxCalculator = new TaxCalculator(DbContext, _workContext, _roundingHelper, _taxService, _taxSettings);

            // INFO: Create real instance of PriceCalculatorFactory with own instances of Calculators
            _priceCalculatorFactory = new PriceCalculatorFactory(_requestCache, GetPriceCalculators(_priceCalculatorFactory, _discountService, _priceSettings));

            var ruleProviderFactoryMock = new Mock<IRuleProviderFactory>();
            ruleProviderFactoryMock.Setup(x => x.GetProvider(RuleScope.Cart, null)).Returns(new Mock<ICartRuleProvider>().Object);

            _shippingService = new ShippingService(
                _productAttributeMaterializer,
                _checkoutAttributeMaterializer,
                ruleProviderFactoryMock.Object,
                _shippingSettings,
                ProviderManager,
                null,
                _roundingHelper,
                _storeContext,
                DbContext);

            _priceCalcService = new PriceCalculationService(
                DbContext,
                _workContext,
                _storeContext,
                _priceCalculatorFactory,
                _taxCalculator,
                _productService,
                _productAttributeMaterializer,
                _taxService,
                _currencyService,
                _roundingHelper,
                priceLabelService.Object,
                _priceSettings,
                _currencySettings,
                _taxSettings);

            _orderCalcService = new OrderCalculationService(
                DbContext,
                _priceCalcService,
                _productService,
                _discountService,
                _shippingService,
                _giftCardService,
                _currencyService,
                _roundingHelper,
                _requestCache,
                ProviderManager,
                _checkoutAttributeMaterializer,
                _workContext,
                _storeContext,
                _taxService,
                _taxCalculator,
                _taxSettings,
                _rewardPointsSettings,
                _priceSettings,
                _shippingSettings);
        }

        [Test]
        public async Task Can_get_shopping_cart_subTotal_excluding_tax()
        {
            var product1 = new Product
            {
                Id = 1,
                Name = "Product name 1",
                Price = 12.34M,
                CustomerEntersPrice = false,
                Published = true,
            };
            var sci1 = new ShoppingCartItem()
            {
                Product = product1,
                ProductId = product1.Id,
                Quantity = 2,
            };
            var product2 = new Product
            {
                Id = 2,
                Name = "Product name 2",
                Price = 21.57M,
                CustomerEntersPrice = false,
                Published = true,
            };
            var sci2 = new ShoppingCartItem()
            {
                Product = product2,
                ProductId = product2.Id,
                Quantity = 3
            };

            var items = new List<OrganizedShoppingCartItem> { new(sci1), new(sci2) };

            items.ForEach(sci => sci.Item.Customer = _customer);
            items.ForEach(sci => sci.Item.CustomerId = _customer.Id);

            var cart = new ShoppingCart(_customer, 0, items);

            _productBatchContext = new ProductBatchContext(new List<Product> { product1, product2 }, DbContext, LifetimeScope, _store, _customer, false);

            //10% - default tax rate
            var subTotal = await _orderCalcService.GetShoppingCartSubtotalAsync(cart, false, _productBatchContext);

            subTotal.DiscountAmount.Amount.ShouldEqual(0);
            subTotal.AppliedDiscount.ShouldBeNull();
            subTotal.SubtotalWithoutDiscount.Amount.ShouldEqual(89.39);
            subTotal.SubtotalWithDiscount.Amount.ShouldEqual(89.39);
            subTotal.TaxRates.Count.ShouldEqual(1);
            subTotal.TaxRates.ContainsKey(10).ShouldBeTrue();
            subTotal.TaxRates[10].ShouldEqual(8.939);
        }

        [Test]
        public async Task Can_get_shopping_cart_subTotal_including_tax()
        {
            var product1 = new Product
            {
                Id = 1,
                Name = "Product name 1",
                Price = 12.34M,
                CustomerEntersPrice = false,
                Published = true,
            };
            var sci1 = new ShoppingCartItem()
            {
                Product = product1,
                ProductId = product1.Id,
                Quantity = 2,
            };
            var product2 = new Product
            {
                Id = 2,
                Name = "Product name 2",
                Price = 21.57M,
                CustomerEntersPrice = false,
                Published = true,
            };
            var sci2 = new ShoppingCartItem()
            {
                Product = product2,
                ProductId = product2.Id,
                Quantity = 3
            };

            var items = new List<OrganizedShoppingCartItem> { new(sci1), new(sci2) };

            items.ForEach(sci => sci.Item.Customer = _customer);
            items.ForEach(sci => sci.Item.CustomerId = _customer.Id);

            var cart = new ShoppingCart(_customer, 0, items);

            _productBatchContext = new ProductBatchContext(new List<Product> { product1, product2 }, DbContext, LifetimeScope, _store, _customer, false);

            //10% - default tax rate
            var subTotal = await _orderCalcService.GetShoppingCartSubtotalAsync(cart, true, _productBatchContext);

            subTotal.DiscountAmount.Amount.ShouldEqual(0);
            subTotal.AppliedDiscount.ShouldBeNull();
            subTotal.SubtotalWithoutDiscount.Amount.ShouldEqual(98.329);
            subTotal.SubtotalWithDiscount.Amount.ShouldEqual(98.329);
            subTotal.TaxRates.Count.ShouldEqual(1);
            subTotal.TaxRates.ContainsKey(10).ShouldBeTrue();
            subTotal.TaxRates[10].ShouldEqual(8.939);
        }

        [Test]
        public async Task Can_get_shopping_cart_subTotal_discount_excluding_tax()
        {
            var product1 = new Product
            {
                Id = 1,
                Name = "Product name 1",
                Price = 12.34M,
                CustomerEntersPrice = false,
                Published = true,
            };
            var sci1 = new ShoppingCartItem
            {
                Product = product1,
                ProductId = product1.Id,
                Quantity = 2,
            };
            var product2 = new Product
            {
                Id = 2,
                Name = "Product name 2",
                Price = 21.57M,
                CustomerEntersPrice = false,
                Published = true,
            };
            var sci2 = new ShoppingCartItem
            {
                Product = product2,
                ProductId = product2.Id,
                Quantity = 3
            };

            var items = new List<OrganizedShoppingCartItem> { new(sci1), new(sci2) };

            items.ForEach(sci => sci.Item.Customer = _customer);
            items.ForEach(sci => sci.Item.CustomerId = _customer.Id);

            var cart = new ShoppingCart(_customer, 0, items);

            // Discounts
            var discount1 = new Discount
            {
                Id = 1,
                Name = "Discount 1",
                DiscountType = DiscountType.AssignedToOrderSubTotal,
                DiscountAmount = 3,
                DiscountLimitation = DiscountLimitationType.Unlimited,
            };

            InitDiscountServiceMock(discount1, DiscountType.AssignedToOrderSubTotal);

            _productBatchContext = new ProductBatchContext(new List<Product> { product1, product2 }, DbContext, LifetimeScope, _store, _customer, false);

            //10% - default tax rate
            var subTotal = await _orderCalcService.GetShoppingCartSubtotalAsync(cart, false, _productBatchContext);

            subTotal.DiscountAmount.Amount.ShouldEqual(3);
            subTotal.AppliedDiscount.ShouldNotBeNull();
            subTotal.AppliedDiscount.Name.ShouldEqual("Discount 1");

            subTotal.SubtotalWithoutDiscount.Amount.ShouldEqual(89.39);
            subTotal.SubtotalWithDiscount.Amount.ShouldEqual(86.39);
            subTotal.TaxRates.Count.ShouldEqual(1);
            subTotal.TaxRates.ContainsKey(10).ShouldBeTrue();
            subTotal.TaxRates[10].ShouldEqual(8.639);
        }

        [Test]
        public async Task Can_get_shopping_cart_subTotal_discount_including_tax()
        {
            var product1 = new Product
            {
                Id = 1,
                Name = "Product name 1",
                Price = 12.34M,
                CustomerEntersPrice = false,
                Published = true,
            };
            var sci1 = new ShoppingCartItem
            {
                Product = product1,
                ProductId = product1.Id,
                Quantity = 2,
            };
            var product2 = new Product
            {
                Id = 2,
                Name = "Product name 2",
                Price = 21.57M,
                CustomerEntersPrice = false,
                Published = true,
            };
            var sci2 = new ShoppingCartItem
            {
                Product = product2,
                ProductId = product2.Id,
                Quantity = 3
            };

            var items = new List<OrganizedShoppingCartItem> { new(sci1), new(sci2) };

            items.ForEach(sci => sci.Item.Customer = _customer);
            items.ForEach(sci => sci.Item.CustomerId = _customer.Id);

            var cart = new ShoppingCart(_customer, 0, items);

            //discounts
            var discount1 = new Discount
            {
                Id = 1,
                Name = "Discount 1",
                DiscountType = DiscountType.AssignedToOrderSubTotal,
                DiscountAmount = 3,
                DiscountLimitation = DiscountLimitationType.Unlimited,
            };

            InitDiscountServiceMock(discount1, DiscountType.AssignedToOrderSubTotal);

            _productBatchContext = new ProductBatchContext(new List<Product> { product1, product2 }, DbContext, LifetimeScope, _store, _customer, false);

            var subTotal = await _orderCalcService.GetShoppingCartSubtotalAsync(cart, true, _productBatchContext);

            (3.3M == Math.Round(subTotal.DiscountAmount.Amount, 8)).ShouldBeTrue();
            subTotal.AppliedDiscount.ShouldNotBeNull();
            subTotal.AppliedDiscount.Name.ShouldEqual("Discount 1");
            subTotal.SubtotalWithoutDiscount.Amount.ShouldEqual(98.329);
            subTotal.SubtotalWithDiscount.Amount.ShouldEqual(95.029);
            subTotal.TaxRates.Count.ShouldEqual(1);
            subTotal.TaxRates.ContainsKey(10).ShouldBeTrue();
            subTotal.TaxRates[10].ShouldEqual(8.639);
        }

        [Test]
        public async Task Can_get_shoppingCartItem_additional_shippingCharge()
        {
            var sci1 = new ShoppingCartItem()
            {
                RawAttributes = string.Empty,
                Quantity = 3,
                Product = new Product()
                {
                    Weight = 1.5M,
                    Height = 2.5M,
                    Length = 3.5M,
                    Width = 4.5M,
                    AdditionalShippingCharge = 5.5M,
                    IsShippingEnabled = true,
                }
            };
            var sci2 = new ShoppingCartItem()
            {
                RawAttributes = string.Empty,
                Quantity = 4,
                Product = new Product()
                {
                    Weight = 11.5M,
                    Height = 12.5M,
                    Length = 13.5M,
                    Width = 14.5M,
                    AdditionalShippingCharge = 6.5M,
                    IsShippingEnabled = true,
                }
            };

            //sci3 is not shippable
            var sci3 = new ShoppingCartItem()
            {
                RawAttributes = string.Empty,
                Quantity = 5,
                Product = new Product()
                {
                    Weight = 11.5M,
                    Height = 12.5M,
                    Length = 13.5M,
                    Width = 14.5M,
                    AdditionalShippingCharge = 7.5M,
                    IsShippingEnabled = false,
                }
            };

            var items = new List<OrganizedShoppingCartItem>
            {
                new OrganizedShoppingCartItem(sci1),
                new OrganizedShoppingCartItem(sci2),
                new OrganizedShoppingCartItem(sci3)
            };

            var cart = new ShoppingCart(_customer, 0, items);

            (await _orderCalcService.GetShoppingCartShippingChargeAsync(cart)).Amount.ShouldEqual(42.5M);
        }

        [Test]
        public async Task Shipping_should_be_free_when_all_shoppingCartItems_are_marked_as_freeShipping()
        {
            var sci1 = new ShoppingCartItem()
            {
                RawAttributes = string.Empty,
                Quantity = 3,
                Product = new Product()
                {
                    Weight = 1.5M,
                    Height = 2.5M,
                    Length = 3.5M,
                    Width = 4.5M,
                    IsFreeShipping = true,
                    IsShippingEnabled = true,
                }
            };
            var sci2 = new ShoppingCartItem()
            {
                RawAttributes = string.Empty,
                Quantity = 4,
                Product = new Product()
                {
                    Weight = 11.5M,
                    Height = 12.5M,
                    Length = 13.5M,
                    Width = 14.5M,
                    IsFreeShipping = true,
                    IsShippingEnabled = true,
                }
            };

            var items = new List<OrganizedShoppingCartItem>
            {
                new OrganizedShoppingCartItem(sci1),
                new OrganizedShoppingCartItem(sci2)
            };

            items.ForEach(sci => sci.Item.Customer = _customer);
            items.ForEach(sci => sci.Item.CustomerId = _customer.Id);

            var cart = new ShoppingCart(_customer, 0, items);

            (await _orderCalcService.IsFreeShippingAsync(cart)).ShouldEqual(true);
        }

        [Test]
        public async Task Shipping_should_not_be_free_when_some_of_shoppingCartItems_are_not_marked_as_freeShipping()
        {
            var sci1 = new ShoppingCartItem()
            {
                RawAttributes = string.Empty,
                Quantity = 3,
                Product = new Product()
                {
                    Weight = 1.5M,
                    Height = 2.5M,
                    Length = 3.5M,
                    Width = 4.5M,
                    IsFreeShipping = true,
                    IsShippingEnabled = true,
                }
            };
            var sci2 = new ShoppingCartItem()
            {
                RawAttributes = string.Empty,
                Quantity = 4,
                Product = new Product()
                {
                    Weight = 11.5M,
                    Height = 12.5M,
                    Length = 13.5M,
                    Width = 14.5M,
                    IsFreeShipping = false,
                    IsShippingEnabled = true,
                }
            };

            var items = new List<OrganizedShoppingCartItem>
            {
                new OrganizedShoppingCartItem(sci1),
                new OrganizedShoppingCartItem(sci2)
            };

            items.ForEach(sci => sci.Item.Customer = _customer);
            items.ForEach(sci => sci.Item.CustomerId = _customer.Id);

            var cart = new ShoppingCart(_customer, 0, items);

            (await _orderCalcService.IsFreeShippingAsync(cart)).ShouldEqual(false);
        }

        [Test]
        public async Task Shipping_should_be_free_when_customer_is_in_role_with_free_shipping()
        {
            var sci1 = new ShoppingCartItem()
            {
                RawAttributes = string.Empty,
                Quantity = 3,
                Product = new Product()
                {
                    Weight = 1.5M,
                    Height = 2.5M,
                    Length = 3.5M,
                    Width = 4.5M,
                    IsFreeShipping = false,
                    IsShippingEnabled = true,
                }
            };
            var sci2 = new ShoppingCartItem()
            {
                RawAttributes = string.Empty,
                Quantity = 4,
                Product = new Product()
                {
                    Weight = 11.5M,
                    Height = 12.5M,
                    Length = 13.5M,
                    Width = 14.5M,
                    IsFreeShipping = false,
                    IsShippingEnabled = true,
                }
            };

            var items = new List<OrganizedShoppingCartItem>
            {
                new OrganizedShoppingCartItem(sci1),
                new OrganizedShoppingCartItem(sci2)
            };

            var customer = new Customer();
            var customerRole1 = new CustomerRole
            {
                Active = true,
                FreeShipping = true,
            };
            var customerRole2 = new CustomerRole
            {
                Active = true,
                FreeShipping = false,
            };

            customer.CustomerRoleMappings.Add(new CustomerRoleMapping
            {
                CustomerId = customer.Id,
                CustomerRole = customerRole1
            });
            customer.CustomerRoleMappings.Add(new CustomerRoleMapping
            {
                CustomerId = customer.Id,
                CustomerRole = customerRole2
            });

            items.ForEach(sci => sci.Item.Customer = customer);
            items.ForEach(sci => sci.Item.CustomerId = customer.Id);

            var cart = new ShoppingCart(customer, 0, items);

            (await _orderCalcService.IsFreeShippingAsync(cart)).ShouldEqual(true);
        }

        [Test]
        public async Task Can_get_shipping_total_with_fixed_shipping_rate_excluding_tax()
        {
            var sci1 = new ShoppingCartItem()
            {
                RawAttributes = string.Empty,
                Quantity = 3,
                Product = new Product()
                {
                    Id = 1,
                    Weight = 1.5M,
                    Height = 2.5M,
                    Length = 3.5M,
                    Width = 4.5M,
                    AdditionalShippingCharge = 5.5M,
                    IsShippingEnabled = true,
                }
            };
            var sci2 = new ShoppingCartItem()
            {
                RawAttributes = string.Empty,
                Quantity = 4,
                Product = new Product()
                {
                    Id = 2,
                    Weight = 11.5M,
                    Height = 12.5M,
                    Length = 13.5M,
                    Width = 14.5M,
                    AdditionalShippingCharge = 6.5M,
                    IsShippingEnabled = true,
                }
            };

            //sci3 is not shippable
            var sci3 = new ShoppingCartItem()
            {
                RawAttributes = string.Empty,
                Quantity = 5,
                Product = new Product()
                {
                    Id = 3,
                    Weight = 11.5M,
                    Height = 12.5M,
                    Length = 13.5M,
                    Width = 14.5M,
                    AdditionalShippingCharge = 7.5M,
                    IsShippingEnabled = false,
                }
            };

            var items = new List<OrganizedShoppingCartItem>
            {
                new OrganizedShoppingCartItem(sci1),
                new OrganizedShoppingCartItem(sci2),
                new OrganizedShoppingCartItem(sci3)
            };

            items.ForEach(sci => sci.Item.Customer = _customer);
            items.ForEach(sci => sci.Item.CustomerId = _customer.Id);

            var cart = new ShoppingCart(_customer, 0, items);

            var shipping = await _orderCalcService.GetShoppingCartShippingTotalAsync(cart, false);
            shipping.ShippingTotal.ShouldNotBeNull();
            //10 - default fixed shipping rate, 42.5 - additional shipping change
            shipping.ShippingTotal.Value.Amount.ShouldEqual(52.5);
            shipping.AppliedDiscount.ShouldBeNull();
            //10 - default fixed tax rate
            shipping.TaxRate.ShouldEqual(10);
        }

        [Test]
        public async Task Can_get_shipping_total_with_fixed_shipping_rate_including_tax()
        {
            var sci1 = new ShoppingCartItem()
            {
                RawAttributes = string.Empty,
                Quantity = 3,
                Product = new Product()
                {
                    Id = 1,
                    Weight = 1.5M,
                    Height = 2.5M,
                    Length = 3.5M,
                    Width = 4.5M,
                    AdditionalShippingCharge = 5.5M,
                    IsShippingEnabled = true,
                }
            };
            var sci2 = new ShoppingCartItem()
            {
                RawAttributes = string.Empty,
                Quantity = 4,
                Product = new Product()
                {
                    Id = 2,
                    Weight = 11.5M,
                    Height = 12.5M,
                    Length = 13.5M,
                    Width = 14.5M,
                    AdditionalShippingCharge = 6.5M,
                    IsShippingEnabled = true,
                }
            };

            //sci3 is not shippable
            var sci3 = new ShoppingCartItem()
            {
                RawAttributes = string.Empty,
                Quantity = 5,
                Product = new Product()
                {
                    Id = 3,
                    Weight = 11.5M,
                    Height = 12.5M,
                    Length = 13.5M,
                    Width = 14.5M,
                    AdditionalShippingCharge = 7.5M,
                    IsShippingEnabled = false,
                }
            };

            var items = new List<OrganizedShoppingCartItem>
            {
                new OrganizedShoppingCartItem(sci1),
                new OrganizedShoppingCartItem(sci2),
                new OrganizedShoppingCartItem(sci3)
            };

            items.ForEach(sci => sci.Item.Customer = _customer);
            items.ForEach(sci => sci.Item.CustomerId = _customer.Id);

            var cart = new ShoppingCart(_customer, 0, items);

            var shipping = await _orderCalcService.GetShoppingCartShippingTotalAsync(cart, true);
            shipping.ShippingTotal.ShouldNotBeNull();
            //10 - default fixed shipping rate, 42.5 - additional shipping change
            shipping.ShippingTotal.Value.Amount.ShouldEqual(57.75);
            shipping.AppliedDiscount.ShouldBeNull();
            //10 - default fixed tax rate
            shipping.TaxRate.ShouldEqual(10);
        }

        [Test]
        public async Task Can_get_shipping_total_discount_excluding_tax()
        {
            var sci1 = new ShoppingCartItem
            {
                RawAttributes = string.Empty,
                Quantity = 3,
                Product = new Product
                {
                    Id = 1,
                    Weight = 1.5M,
                    Height = 2.5M,
                    Length = 3.5M,
                    Width = 4.5M,
                    AdditionalShippingCharge = 5.5M,
                    IsShippingEnabled = true,
                }
            };
            var sci2 = new ShoppingCartItem
            {
                RawAttributes = string.Empty,
                Quantity = 4,
                Product = new Product
                {
                    Id = 2,
                    Weight = 11.5M,
                    Height = 12.5M,
                    Length = 13.5M,
                    Width = 14.5M,
                    AdditionalShippingCharge = 6.5M,
                    IsShippingEnabled = true,
                }
            };

            //sci3 is not shippable
            var sci3 = new ShoppingCartItem
            {
                RawAttributes = string.Empty,
                Quantity = 5,
                Product = new Product
                {
                    Id = 3,
                    Weight = 11.5M,
                    Height = 12.5M,
                    Length = 13.5M,
                    Width = 14.5M,
                    AdditionalShippingCharge = 7.5M,
                    IsShippingEnabled = false,
                }
            };

            var items = new List<OrganizedShoppingCartItem> { new(sci1), new(sci2), new(sci3) };

            items.ForEach(sci => sci.Item.Customer = _customer);
            items.ForEach(sci => sci.Item.CustomerId = _customer.Id);

            var cart = new ShoppingCart(_customer, 0, items);

            //discounts
            var discount1 = new Discount
            {
                Id = 1,
                Name = "Discount 1",
                DiscountType = DiscountType.AssignedToShipping,
                DiscountAmount = 3,
                DiscountLimitation = DiscountLimitationType.Unlimited,
            };

            InitDiscountServiceMock(discount1, DiscountType.AssignedToShipping);

            var shipping = await _orderCalcService.GetShoppingCartShippingTotalAsync(cart, false);
            shipping.AppliedDiscount.ShouldNotBeNull();
            shipping.AppliedDiscount.Name.ShouldEqual("Discount 1");
            shipping.ShouldNotBeNull();
            //10 - default fixed shipping rate, 42.5 - additional shipping change, -3 - discount
            shipping.ShippingTotal.Value.Amount.ShouldEqual(49.5);
            //10 - default fixed tax rate
            shipping.TaxRate.ShouldEqual(10);
        }

        [Test]
        public async Task Can_get_shipping_total_discount_including_tax()
        {
            var sci1 = new ShoppingCartItem()
            {
                RawAttributes = string.Empty,
                Quantity = 3,
                Product = new Product()
                {
                    Id = 1,
                    Weight = 1.5M,
                    Height = 2.5M,
                    Length = 3.5M,
                    Width = 4.5M,
                    AdditionalShippingCharge = 5.5M,
                    IsShippingEnabled = true,
                }
            };
            var sci2 = new ShoppingCartItem()
            {
                RawAttributes = string.Empty,
                Quantity = 4,
                Product = new Product()
                {
                    Id = 2,
                    Weight = 11.5M,
                    Height = 12.5M,
                    Length = 13.5M,
                    Width = 14.5M,
                    AdditionalShippingCharge = 6.5M,
                    IsShippingEnabled = true,
                }
            };

            //sci3 is not shippable
            var sci3 = new ShoppingCartItem()
            {
                RawAttributes = string.Empty,
                Quantity = 5,
                Product = new Product()
                {
                    Id = 3,
                    Weight = 11.5M,
                    Height = 12.5M,
                    Length = 13.5M,
                    Width = 14.5M,
                    AdditionalShippingCharge = 7.5M,
                    IsShippingEnabled = false,
                }
            };

            var items = new List<OrganizedShoppingCartItem>
            {
                new OrganizedShoppingCartItem(sci1),
                new OrganizedShoppingCartItem(sci2),
                new OrganizedShoppingCartItem(sci3)
            };

            items.ForEach(sci => sci.Item.Customer = _customer);
            items.ForEach(sci => sci.Item.CustomerId = _customer.Id);

            var cart = new ShoppingCart(_customer, 0, items);

            //discounts
            var discount1 = new Discount()
            {
                Id = 1,
                Name = "Discount 1",
                DiscountType = DiscountType.AssignedToShipping,
                DiscountAmount = 3,
                DiscountLimitation = DiscountLimitationType.Unlimited,
            };

            InitDiscountServiceMock(discount1, DiscountType.AssignedToShipping);

            var shipping = await _orderCalcService.GetShoppingCartShippingTotalAsync(cart, true);
            shipping.AppliedDiscount.ShouldNotBeNull();
            shipping.AppliedDiscount.Name.ShouldEqual("Discount 1");
            shipping.ShippingTotal.Value.ShouldNotBeNull();
            //10 - default fixed shipping rate, 42.5 - additional shipping change, -3 - discount
            shipping.ShippingTotal.Value.Amount.ShouldEqual(54.45);
            //10 - default fixed tax rate
            shipping.TaxRate.ShouldEqual(10);
        }

        [Test]
        public async Task Can_get_tax_total()
        {
            //customer
            var customer = new Customer
            {
                Id = 10,
            };

            //shopping cart
            var product1 = new Product
            {
                Id = 1,
                Name = "Product name 1",
                Price = 10M,
                Published = true,
                IsShippingEnabled = true,
            };
            var sci1 = new ShoppingCartItem()
            {
                Product = product1,
                ProductId = product1.Id,
                Quantity = 2,
            };
            var product2 = new Product
            {
                Id = 2,
                Name = "Product name 2",
                Price = 12M,
                Published = true,
                IsShippingEnabled = true,
            };
            var sci2 = new ShoppingCartItem()
            {
                Product = product2,
                ProductId = product2.Id,
                Quantity = 3
            };

            var items = new List<OrganizedShoppingCartItem>
            {
                new OrganizedShoppingCartItem(sci1),
                new OrganizedShoppingCartItem(sci2)
            };

            items.ForEach(sci => sci.Item.Customer = _customer);
            items.ForEach(sci => sci.Item.CustomerId = _customer.Id);

            var cart = new ShoppingCart(_customer, 0, items);

            //_genericAttributeService.Expect(x => x.GetAttributesForEntity(customer.Id, "Customer"))
            //	.Return(new List<GenericAttribute>()
            //				{
            //					new GenericAttribute()
            //						{
            //							StoreId = _store.Id,
            //							EntityId = customer.Id,
            //							Key = SystemCustomerAttributeNames.SelectedPaymentMethod,
            //							KeyGroup = "Customer",
            //							Value = "test1"
            //						}
            //				});
            //_paymentService.Expect(ps => ps.GetAdditionalHandlingFee(cart, "test1")).Return(20);

            //56 - items, 10 - shipping (fixed), 20 - payment fee = 86
            //56 - items, 10 - shipping (fixed) = 66

            //1. shipping is taxable, payment fee is taxable
            _taxSettings.ShippingIsTaxable = true;
            _taxSettings.PaymentMethodAdditionalFeeIsTaxable = true;

            var taxTotal = await _orderCalcService.GetShoppingCartTaxTotalAsync(cart);
            taxTotal.Price.Amount.ShouldEqual(6.6m);
            taxTotal.TaxRates.ShouldNotBeNull();
            taxTotal.TaxRates.Count.ShouldEqual(1);
            taxTotal.TaxRates.ContainsKey(10).ShouldBeTrue();
            taxTotal.TaxRates[10].ShouldEqual(6.6);

            //2. shipping is taxable, payment fee is not taxable
            _taxSettings.ShippingIsTaxable = true;
            _taxSettings.PaymentMethodAdditionalFeeIsTaxable = false;

            taxTotal = await _orderCalcService.GetShoppingCartTaxTotalAsync(cart);
            taxTotal.Price.Amount.ShouldEqual(6.6m);
            taxTotal.TaxRates.ShouldNotBeNull();
            taxTotal.TaxRates.Count.ShouldEqual(1);
            taxTotal.TaxRates.ContainsKey(10).ShouldBeTrue();
            taxTotal.TaxRates[10].ShouldEqual(6.6);

            //3. shipping is not taxable, payment fee is taxable
            _taxSettings.ShippingIsTaxable = false;
            _taxSettings.PaymentMethodAdditionalFeeIsTaxable = true;

            taxTotal = await _orderCalcService.GetShoppingCartTaxTotalAsync(cart);
            taxTotal.Price.Amount.ShouldEqual(5.6);
            taxTotal.TaxRates.ShouldNotBeNull();
            taxTotal.TaxRates.Count.ShouldEqual(1);
            taxTotal.TaxRates.ContainsKey(10).ShouldBeTrue();
            taxTotal.TaxRates[10].ShouldEqual(5.6);

            //3. shipping is not taxable, payment fee is not taxable
            _taxSettings.ShippingIsTaxable = false;
            _taxSettings.PaymentMethodAdditionalFeeIsTaxable = false;

            taxTotal = await _orderCalcService.GetShoppingCartTaxTotalAsync(cart);
            taxTotal.Price.Amount.ShouldEqual(5.6);
            taxTotal.TaxRates.ShouldNotBeNull();
            taxTotal.TaxRates.Count.ShouldEqual(1);
            taxTotal.TaxRates.ContainsKey(10).ShouldBeTrue();
            taxTotal.TaxRates[10].ShouldEqual(5.6);
        }

        //[Test]
        //public void Can_get_shopping_cart_total_without_shipping_required()
        //{
        //	//customer
        //	var customer = new Customer()
        //	{
        //		Id = 10,
        //	};

        //	//shopping cart
        //	var product1 = new Product
        //	{
        //		Id = 1,
        //		Name = "Product name 1",
        //		Price = 10M,
        //		Published = true,
        //		IsShipEnabled = false,
        //	};
        //	var sci1 = new ShoppingCartItem()
        //	{
        //		Product = product1,
        //		ProductId = product1.Id,
        //		Quantity = 2,
        //	};
        //	var product2 = new Product
        //	{
        //		Id = 2,
        //		Name = "Product name 2",
        //		Price = 12M,
        //		Published = true,
        //		IsShipEnabled = false,
        //	};
        //	var sci2 = new ShoppingCartItem()
        //	{
        //		Product = product2,
        //		ProductId = product2.Id,
        //		Quantity = 3
        //	};

        //	var cart = new List<ShoppingCartItem>() { sci1, sci2 };
        //	cart.ForEach(sci => sci.Customer = customer);
        //	cart.ForEach(sci => sci.CustomerId = customer.Id);



        //	_genericAttributeService.Expect(x => x.GetAttributesForEntity(customer.Id, "Customer"))
        //		.Return(new List<GenericAttribute>()
        //					{
        //						new GenericAttribute()
        //							{
        //								StoreId = _store.Id,
        //								EntityId = customer.Id,
        //								Key = SystemCustomerAttributeNames.SelectedPaymentMethod,
        //								KeyGroup = "Customer",
        //								Value = "test1"
        //							}
        //					});
        //	_paymentService.Expect(ps => ps.GetAdditionalHandlingFee(cart, "test1")).Return(20);

        //	_discountService.Expect(ds => ds.GetAllDiscounts(DiscountType.AssignedToCategories)).Return(new List<Discount>());

        //	decimal discountAmount;
        //	Discount appliedDiscount;
        //	List<AppliedGiftCard> appliedGiftCards;
        //	int redeemedRewardPoints;
        //	decimal redeemedRewardPointsAmount;


        //	//shipping is taxable, payment fee is taxable
        //	_taxSettings.ShippingIsTaxable = true;
        //	_taxSettings.PaymentMethodAdditionalFeeIsTaxable = true;

        //	//56 - items, 20 - payment fee, 7.6 - tax
        //	_orderTotalCalcService.GetShoppingCartTotal(cart,  out discountAmount, out appliedDiscount, 
        //		out appliedGiftCards, out redeemedRewardPoints, out redeemedRewardPointsAmount)
        //		.ShouldEqual(83.6M);
        //}

        //[Test]
        //public void Can_get_shopping_cart_total_with_shipping_required()
        //{
        //	//customer
        //	var customer = new Customer()
        //	{
        //		Id = 10,
        //	};

        //	//shopping cart
        //	var product1 = new Product
        //	{
        //		Id = 1,
        //		Name = "Product name 1",
        //		Price = 10M,
        //		Published = true,
        //		IsShipEnabled = true,
        //	};
        //	var sci1 = new ShoppingCartItem()
        //	{
        //		Product = product1,
        //		ProductId = product1.Id,
        //		Quantity = 2,
        //	};
        //	var product2 = new Product
        //	{
        //		Id = 2,
        //		Name = "Product name 2",
        //		Price = 12M,
        //		Published = true,
        //		IsShipEnabled = true,
        //	};
        //	var sci2 = new ShoppingCartItem()
        //	{
        //		Product = product2,
        //		ProductId = product2.Id,
        //		Quantity = 3
        //	};

        //	var cart = new List<ShoppingCartItem>() { sci1, sci2 };
        //	cart.ForEach(sci => sci.Customer = customer);
        //	cart.ForEach(sci => sci.CustomerId = customer.Id);

        //	_genericAttributeService.Expect(x => x.GetAttributesForEntity(customer.Id, "Customer"))
        //		.Return(new List<GenericAttribute>()
        //					{
        //						new GenericAttribute()
        //							{
        //								StoreId = _store.Id,
        //								EntityId = customer.Id,
        //								Key = SystemCustomerAttributeNames.SelectedPaymentMethod,
        //								KeyGroup = "Customer",
        //								Value = "test1"
        //							}
        //					});
        //	_paymentService.Expect(ps => ps.GetAdditionalHandlingFee(cart, "test1")).Return(20);

        //	_discountService.Expect(ds => ds.GetAllDiscounts(DiscountType.AssignedToCategories)).Return(new List<Discount>());

        //	decimal discountAmount;
        //	Discount appliedDiscount;
        //	List<AppliedGiftCard> appliedGiftCards;
        //	int redeemedRewardPoints;
        //	decimal redeemedRewardPointsAmount;


        //	//shipping is taxable, payment fee is taxable
        //	_taxSettings.ShippingIsTaxable = true;
        //	_taxSettings.PaymentMethodAdditionalFeeIsTaxable = true;

        //	//56 - items, 10 - shipping (fixed), 20 - payment fee, 8.6 - tax
        //	_orderTotalCalcService.GetShoppingCartTotal(cart, out discountAmount, out appliedDiscount,
        //		out appliedGiftCards, out redeemedRewardPoints, out redeemedRewardPointsAmount)
        //		.ShouldEqual(94.6M);
        //}

        //[Test]
        //public void Can_get_shopping_cart_total_with_applied_reward_points()
        //{
        //   //customer
        //	var customer = new Customer()
        //	{
        //		Id = 10,
        //	};

        //	//shopping cart
        //	var product1 = new Product
        //	{
        //		Id = 1,
        //		Name = "Product name 1",
        //		Price = 10M,
        //		Published = true,
        //		IsShipEnabled = true,
        //	};
        //	var sci1 = new ShoppingCartItem()
        //	{
        //		Product = product1,
        //		ProductId = product1.Id,
        //		Quantity = 2,
        //	};
        //	var product2 = new Product
        //	{
        //		Id = 2,
        //		Name = "Product name 2",
        //		Price = 12M,
        //		Published = true,
        //		IsShipEnabled = true,
        //	};
        //	var sci2 = new ShoppingCartItem()
        //	{
        //		Product = product2,
        //		ProductId = product2.Id,
        //		Quantity = 3
        //	};

        //	var cart = new List<ShoppingCartItem>() { sci1, sci2 };
        //	cart.ForEach(sci => sci.Customer = customer);
        //	cart.ForEach(sci => sci.CustomerId = customer.Id);



        //	_genericAttributeService.Expect(x => x.GetAttributesForEntity(customer.Id, "Customer"))
        //		.Return(new List<GenericAttribute>()
        //					{
        //						new GenericAttribute()
        //							{
        //								StoreId = _store.Id,
        //								EntityId = customer.Id,
        //								Key = SystemCustomerAttributeNames.SelectedPaymentMethod,
        //								KeyGroup = "Customer",
        //								Value = "test1"
        //							},
        //						new GenericAttribute()
        //								{
        //								StoreId = 1,
        //								EntityId = customer.Id,
        //								Key = SystemCustomerAttributeNames.UseRewardPointsDuringCheckout,
        //								KeyGroup = "Customer",
        //								Value = true.ToString()
        //								}
        //					});
        //	_paymentService.Expect(ps => ps.GetAdditionalHandlingFee(cart, "test1")).Return(20);


        //	_discountService.Expect(ds => ds.GetAllDiscounts(DiscountType.AssignedToCategories)).Return(new List<Discount>());

        //	decimal discountAmount;
        //	Discount appliedDiscount;
        //	List<AppliedGiftCard> appliedGiftCards;
        //	int redeemedRewardPoints;
        //	decimal redeemedRewardPointsAmount;


        //	//shipping is taxable, payment fee is taxable
        //	_taxSettings.ShippingIsTaxable = true;
        //	_taxSettings.PaymentMethodAdditionalFeeIsTaxable = true;

        //	//reward points
        //	_rewardPointsSettings.Enabled = true;
        //	_rewardPointsSettings.ExchangeRate = 2; //1 reward point = 2
        //	customer.AddRewardPointsHistoryEntry(15); //15*2=30

        //	//56 - items, 10 - shipping (fixed), 20 - payment fee, 8.6 - tax, -30 (reward points)
        //	_orderTotalCalcService.GetShoppingCartTotal(cart, out discountAmount, out appliedDiscount,
        //		out appliedGiftCards, out redeemedRewardPoints, out redeemedRewardPointsAmount)
        //		.ShouldEqual(64.6M);
        //}

        [Test]
        public async Task Can_get_shopping_cart_total()
        {
            var product1 = new Product
            {
                Id = 1,
                Name = "Product name 1",
                Price = 10M,
                Published = true,
                IsShippingEnabled = true,
            };
            var sci1 = new ShoppingCartItem
            {
                Product = product1,
                ProductId = product1.Id,
                Quantity = 2,
                Customer = _customer,
                CustomerId = _customer.Id
            };

            var product2 = new Product
            {
                Id = 2,
                Name = "Product name 2",
                Price = 12M,
                Published = true,
                IsShippingEnabled = true,
            };
            var sci2 = new ShoppingCartItem
            {
                Product = product2,
                ProductId = product2.Id,
                Quantity = 3,
                Customer = _customer,
                CustomerId = _customer.Id
            };

            var items = new List<OrganizedShoppingCartItem>
            {
                new OrganizedShoppingCartItem(sci1),
                new OrganizedShoppingCartItem(sci2)
            };

            var cart = new ShoppingCart(_customer, 0, items);

            // Discounts
            var discount1 = new Discount
            {
                Id = 1,
                Name = "Discount 1",
                DiscountType = DiscountType.AssignedToOrderTotal,
                DiscountAmount = 3,
                DiscountLimitation = DiscountLimitationType.Unlimited,
            };

            InitDiscountServiceMock(discount1, DiscountType.AssignedToOrderTotal);

            //_genericAttributeService.Expect(x => x.GetAttributesForEntity(customer.Id, "Customer"))
            //	.Return(new List<GenericAttribute>
            //	{
            //		new GenericAttribute
            //		{
            //			StoreId = _store.Id,
            //			EntityId = customer.Id,
            //			Key = SystemCustomerAttributeNames.SelectedPaymentMethod,
            //			KeyGroup = "Customer",
            //			Value = "test1"
            //		}
            //	});

            //_paymentService.Expect(ps => ps.GetAdditionalHandlingFee(cart, "test1")).Return(20);

            // Shipping is taxable, payment fee is taxable
            _taxSettings.ShippingIsTaxable = true;
            _taxSettings.PaymentMethodAdditionalFeeIsTaxable = true;

            // 56 - items, 10 - shipping (fixed), 20 - payment fee, 8.6 - tax, [-3] - discount = 91.6
            // 56 - items, 10 - shipping (fixed), 6.6 - tax, [-3] - discount = 69.6
            var cartTotal = await _orderCalcService.GetShoppingCartTotalAsync(cart);
            cartTotal.Total.Value.Amount.ShouldEqual(69.6M);
            cartTotal.DiscountAmount.Amount.ShouldEqual(3);
            cartTotal.AppliedDiscount.ShouldNotBeNull();
            cartTotal.AppliedDiscount.Name.ShouldEqual("Discount 1");
        }

        [Test]
        public void Can_convert_reward_points_to_amount()
        {
            _rewardPointsSettings.Enabled = true;
            _rewardPointsSettings.ExchangeRate = 15M;

            _orderCalcService.ConvertRewardPointsToAmount(100).Amount.ShouldEqual(1500);
        }

        [Test]
        public void Can_convert_amount_to_reward_points()
        {
            _rewardPointsSettings.Enabled = true;
            _rewardPointsSettings.ExchangeRate = 15M;

            //we calculate ceiling for reward points
            _orderCalcService.ConvertAmountToRewardPoints(new Money(100, _currency)).ShouldEqual(7);
        }

        private void InitDiscountServiceMock(Discount discount, DiscountType type)
        {
            _discountServiceMock
                .Setup(x => x.IsDiscountValidAsync(discount, _customer, It.IsAny<string>(), It.IsAny<Store>(), DiscountValidationFlags.All))
                .ReturnsAsync(true);

            _discountServiceMock
                .Setup(x => x.GetAllDiscountsAsync(type, It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<Discount> { discount });
        }
    }
}