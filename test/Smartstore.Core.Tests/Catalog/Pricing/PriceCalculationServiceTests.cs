using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Smartstore.Caching;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Core.Tests.Catalog.Pricing.Calculators;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Catalog.Pricing
{
    // TODO: (mh) (core) Whatever you add to the DbContext here, you must remove it after test is done!

    [TestFixture]
    public class PriceCalculationServiceTests : ServiceTestBase
    {
        IPriceCalculationService _priceCalcService;

        IStoreContext _storeContext;
        IWorkContext _workContext;
        IPriceCalculatorFactory _priceCalculatorFactory;
        ITaxCalculator _taxCalculator;
        IProductAttributeMaterializer _productAttributeMaterializer;
        IProductService _productService;
        IDiscountService _discountService;

        ICurrencyService _currencyService;
        IRoundingHelper _roundingHelper;
        ITaxService _taxService;
        IRequestCache _requestCache;

        CurrencySettings _currencySettings;
        PriceSettings _priceSettings;
        TaxSettings _taxSettings;
        Store _store;
        Currency _currency;
        Customer _customer;
        Language _language;
        Product _product;
        ShoppingCartItem _sci;

        PriceCalculationContext _priceCalculationContext;
        ProductBatchContext _productBatchContext;

        [OneTimeSetUp]
        public new void SetUp()
        {
            _store = new Store { Id = 1 };
            _currency = new Currency { Id = 1 };
            _customer = new Customer { Id = 1 };
            _language = new Language { Id = 1 };

            _requestCache = new NullRequestCache();

            var storeContextMock = new Mock<IStoreContext>();
            _storeContext = storeContextMock.Object;
            storeContextMock.Setup(x => x.CurrentStore).Returns(_store);

            var workContextMock = new Mock<IWorkContext>();
            _workContext = workContextMock.Object;
            workContextMock.Setup(x => x.WorkingCurrency).Returns(_currency);
            workContextMock.Setup(x => x.WorkingLanguage).Returns(_language);

            _currencySettings = new CurrencySettings();
            _priceSettings = new PriceSettings();
            _taxSettings = new TaxSettings();

            var productServiceMock = new Mock<IProductService>();
            _productService = productServiceMock.Object;

            var productAttributeMaterializerMock = new Mock<IProductAttributeMaterializer>();
            _productAttributeMaterializer = productAttributeMaterializerMock.Object;

            var taxServiceMock = new Mock<ITaxService>();
            _taxService = taxServiceMock.Object;

            var discountServiceMock = new Mock<IDiscountService>();
            _discountService = discountServiceMock.Object;

            discountServiceMock
                .Setup(x => x.GetAllDiscountsAsync(DiscountType.AssignedToCategories, It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<Discount>());
            discountServiceMock
                .Setup(x => x.GetAllDiscountsAsync(DiscountType.AssignedToManufacturers, It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<Discount>());

            discountServiceMock
                .Setup(x => x.IsDiscountValidAsync(It.IsAny<Discount>(), _customer, It.IsAny<string>(), It.IsAny<Store>(), It.IsAny<DiscountValidationFlags>()))
                .ReturnsAsync(true);

            var currencyServiceMock = new Mock<ICurrencyService>();
            _currencyService = currencyServiceMock.Object;
            currencyServiceMock.Setup(x => x.PrimaryCurrency).Returns(_currency);
            currencyServiceMock.Setup(x => x.PrimaryExchangeCurrency).Returns(_currency);

            _roundingHelper = new RoundingHelper(_workContext, _currencySettings);

            var priceLabelService = new Mock<IPriceLabelService>();

            // INFO: no mocking here to use real implementation.
            _taxCalculator = new TaxCalculator(DbContext, _workContext, _roundingHelper, _taxService, _taxSettings);

            // INFO: Create real instance of PriceCalculatorFactory with own instances of Calculators
            _priceCalculatorFactory = new PriceCalculatorFactory(_requestCache, base.GetPriceCalculators(_priceCalculatorFactory, _discountService, _priceSettings));

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
        }

        [SetUp]
        public async Task Reset()
        {
            _product = new Product
            {
                Id = 1,
                Name = "Product name 1",
                Price = 12.34M,
                CustomerEntersPrice = false,
                Published = true,
                ProductType = ProductType.SimpleProduct
            };

            _sci = new ShoppingCartItem
            {
                Customer = _customer,
                CustomerId = _customer.Id,
                Product = _product,
                ProductId = _product.Id,
                Quantity = 2,
            };

            _productBatchContext = new ProductBatchContext(new List<Product> { _product }, DbContext, LifetimeScope, _store, _customer, false);
            _priceCalculationContext = new PriceCalculationContext(_product, new PriceCalculationOptions(_productBatchContext, _customer, _store, _language, _currency)
            {
                IgnoreDiscounts = true
            })
            {
                Quantity = 1
            };

            DbContext.Discounts.RemoveRange(DbContext.Discounts);
            DbContext.Products.RemoveRange(DbContext.Products);
            await DbContext.SaveChangesAsync();
        }

        private void InitTierPrices()
        {
            _product.TierPrices.Add(new TierPrice
            {
                Id = 1,
                Price = 10,
                Quantity = 2,
                Product = _product,
                CalculationMethod = TierPriceCalculationMethod.Fixed
            });

            _product.TierPrices.Add(new TierPrice
            {
                Id = 2,
                Price = 8,
                Quantity = 5,
                Product = _product,
                CalculationMethod = TierPriceCalculationMethod.Fixed
            });

            _product.TierPrices.Add(new TierPrice
            {
                Id = 3,
                Price = 1,
                Quantity = 10,
                Product = _product,
                CalculationMethod = TierPriceCalculationMethod.Adjustment
            });

            _product.TierPrices.Add(new TierPrice
            {
                Id = 4,
                Price = 50,
                Quantity = 20,
                Product = _product,
                CalculationMethod = TierPriceCalculationMethod.Percental
            });

            // set HasTierPrices property
            _product.HasTierPrices = true;

            DbContext.Products.Add(_product);
        }

        private void InitTierPricesForCustomerRoles()
        {
            var customerRole1 = new CustomerRole
            {
                Id = 1,
                Name = "Some role 1",
                Active = true,
            };

            var customerRole2 = new CustomerRole
            {
                Id = 2,
                Name = "Some role 2",
                Active = true,
            };

            DbContext.CustomerRoles.Add(customerRole1);
            DbContext.CustomerRoles.Add(customerRole2);

            _product.TierPrices.Add(new TierPrice
            {
                Price = 10,
                Quantity = 2,
                Product = _product,
                CustomerRole = customerRole1,
                CalculationMethod = TierPriceCalculationMethod.Fixed
            });

            _product.TierPrices.Add(new TierPrice
            {
                Price = 9,
                Quantity = 2,
                Product = _product,
                CustomerRole = customerRole2,
                CalculationMethod = TierPriceCalculationMethod.Fixed
            });

            _product.TierPrices.Add(new TierPrice
            {
                Price = 8,
                Quantity = 5,
                Product = _product,
                CustomerRole = customerRole1,
                CalculationMethod = TierPriceCalculationMethod.Fixed
            });

            _product.TierPrices.Add(new TierPrice
            {
                Price = 5,
                Quantity = 10,
                Product = _product,
                CustomerRole = customerRole2,
                CalculationMethod = TierPriceCalculationMethod.Fixed
            });

            // set HasTierPrices property
            _product.HasTierPrices = true;

            DbContext.Products.Add(_product);
            DbContext.Customers.Add(_customer);

            _customer.CustomerRoleMappings.Add(new CustomerRoleMapping
            {
                CustomerId = _customer.Id,
                CustomerRoleId = customerRole1.Id,
                CustomerRole = customerRole1
            });
        }

        [Test]
        public async Task Can_get_final_product_price()
        {
            var price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
            price.FinalPrice.Amount.ShouldEqual(12.34M);

            _priceCalculationContext.Quantity = 2;
            price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);

            price.FinalPrice.Amount.ShouldEqual(12.34M);
        }

        [Test]
        public async Task Can_get_final_product_price_with_tier_prices()
        {
            InitTierPrices();

            await DbContext.SaveChangesAsync();

            _priceCalculationContext.Options.IgnoreDiscounts = false;
            _priceCalculationContext.Options.IgnoreTierPrices = false;

            _priceCalculationContext.Quantity = 1;

            var price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
            price.FinalPrice.Amount.ShouldEqual(12.34M);

            _priceCalculationContext.Quantity = 2;
            price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
            price.FinalPrice.Amount.ShouldEqual(10);

            _priceCalculationContext.Quantity = 3;
            price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
            price.FinalPrice.Amount.ShouldEqual(10);

            _priceCalculationContext.Quantity = 5;
            price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
            price.FinalPrice.Amount.ShouldEqual(8);

            _priceCalculationContext.Quantity = 10;
            price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
            price.FinalPrice.Amount.ShouldEqual(11.34M);

            _priceCalculationContext.Quantity = 20;
            price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
            price.FinalPrice.Amount.ShouldEqual(6.17M);
        }

        [Test]
        public async Task Can_get_final_product_price_with_tier_prices_by_customerRole()
        {
            InitTierPricesForCustomerRoles();

            await DbContext.SaveChangesAsync();

            _priceCalculationContext.Options.IgnoreDiscounts = false;
            _priceCalculationContext.Options.IgnoreTierPrices = false;

            var price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
            price.FinalPrice.Amount.ShouldEqual(12.34M);

            _priceCalculationContext.Quantity = 2;
            price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
            price.FinalPrice.Amount.ShouldEqual(10);

            _priceCalculationContext.Quantity = 3;
            price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
            price.FinalPrice.Amount.ShouldEqual(10);

            _priceCalculationContext.Quantity = 5;
            price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
            price.FinalPrice.Amount.ShouldEqual(8);

            _priceCalculationContext.Quantity = 10;
            price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
            price.FinalPrice.Amount.ShouldEqual(8);
        }

        [Test]
        public async Task Can_apply_percentage_discount_on_tier_price()
        {
            InitTierPrices();

            _priceCalculationContext.Options.IgnoreDiscounts = false;
            _priceCalculationContext.Options.IgnoreTierPrices = false;
            _priceCalculationContext.Options.DiscountValidationFlags = DiscountValidationFlags.None;
            _priceCalculationContext.Options.IgnorePercentageDiscountOnTierPrices = false;
            _priceCalculationContext.Quantity = 5;

            var discount = new Discount
            {
                Id = 1,
                Name = "Discount 1",
                DiscountType = DiscountType.AssignedToSkus,
                UsePercentage = true,
                DiscountPercentage = 10,
                DiscountLimitation = DiscountLimitationType.Unlimited
            };

            discount.AppliedToProducts.Add(_product);
            _product.AppliedDiscounts.Add(discount);
            _product.HasDiscountsApplied = true;

            DbContext.Discounts.Add(discount);
            DbContext.Products.Add(_product);

            await DbContext.SaveChangesAsync();

            var price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);

            // 8.00 (tier price) - 0.80 (10% discount on tier price).
            price.FinalPrice.Amount.ShouldEqual(7.20);
        }

        [Test]
        public async Task Can_get_final_product_price_with_custom_additionalCharge()
        {
            _priceCalculationContext.Metadata[CustomAdditionalChargeCalculator.AdditionalChargeKey] = 5m;

            var price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);

            price.FinalPrice.Amount.ShouldEqual(17.34M);
        }

        [Test]
        public async Task Can_get_final_product_price_with_discount()
        {
            _priceCalculationContext.Options.IgnoreDiscounts = false;
            _priceCalculationContext.Options.DiscountValidationFlags = DiscountValidationFlags.None;

            var discount1 = new Discount()
            {
                Id = 1,
                Name = "Discount 1",
                DiscountType = DiscountType.AssignedToSkus,
                DiscountAmount = 3,
                DiscountLimitation = DiscountLimitationType.Unlimited
            };

            discount1.AppliedToProducts.Add(_product);
            _product.AppliedDiscounts.Add(discount1);
            _product.HasDiscountsApplied = true;

            DbContext.Discounts.Add(discount1);
            DbContext.Products.Add(_product);
            await DbContext.SaveChangesAsync();

            var price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
            price.FinalPrice.Amount.ShouldEqual(9.34M);
        }

        [Test]
        public async Task Can_get_final_product_price_with_special_price()
        {
            _priceCalculationContext.Options.IgnoreOfferPrice = false;

            _priceCalculationContext.Product.SpecialPrice = 10.01M;
            _priceCalculationContext.Product.SpecialPriceStartDateTimeUtc = DateTime.UtcNow.AddDays(-1);
            _priceCalculationContext.Product.SpecialPriceEndDateTimeUtc = DateTime.UtcNow.AddDays(1);

            var price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
            price.FinalPrice.Amount.ShouldEqual(10.01M);

            // Invalid date
            _product.SpecialPriceStartDateTimeUtc = DateTime.UtcNow.AddDays(1);
            price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
            price.FinalPrice.Amount.ShouldEqual(12.34M);

            // No dates
            _product.SpecialPriceStartDateTimeUtc = null;
            _product.SpecialPriceEndDateTimeUtc = null;
            price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
            price.FinalPrice.Amount.ShouldEqual(10.01M);
        }

        [Test]
        public async Task Can_get_final_product_price_with_variant_combination_price()
        {
            var combination = new ProductVariantAttributeCombination
            {
                Id = 1,
                Price = 18.90M,
                ProductId = 1
            };

            // TODO: (mh) (core) Now the product is merged and is never unmerged for the following tests. Learn to ISOLATE each test, this is SOUP!!!!
            _product.MergeWithCombination(combination);

            var price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);
            price.FinalPrice.Amount.ShouldEqual(18.90M);
        }

        [Test]
        public async Task Can_get_product_discount()
        {
            _priceCalculationContext.Options.IgnoreDiscounts = false;
            _priceCalculationContext.Options.DiscountValidationFlags = DiscountValidationFlags.None;

            var discount1 = new Discount()
            {
                Id = 1,
                Name = "Discount 1",
                DiscountType = DiscountType.AssignedToSkus,
                DiscountAmount = 3,
                DiscountLimitation = DiscountLimitationType.Unlimited
            };
            discount1.AppliedToProducts.Add(_product);
            _product.AppliedDiscounts.Add(discount1);
            _product.HasDiscountsApplied = true;

            var discount2 = new Discount()
            {
                Id = 2,
                Name = "Discount 2",
                DiscountType = DiscountType.AssignedToSkus,
                DiscountAmount = 4,
                DiscountLimitation = DiscountLimitationType.Unlimited
            };
            discount2.AppliedToProducts.Add(_product);
            _product.AppliedDiscounts.Add(discount2);

            var discount3 = new Discount()
            {
                Id = 3,
                Name = "Discount 3",
                DiscountType = DiscountType.AssignedToOrderSubTotal,
                DiscountAmount = 5,
                DiscountLimitation = DiscountLimitationType.Unlimited,
                RequiresCouponCode = true,
                CouponCode = "SECRET CODE"
            };
            discount3.AppliedToProducts.Add(_product);
            _product.AppliedDiscounts.Add(discount3);

            DbContext.Discounts.Add(discount1);
            DbContext.Discounts.Add(discount2);
            DbContext.Discounts.Add(discount3);
            DbContext.Products.Add(_product);
            await DbContext.SaveChangesAsync();

            var price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);

            price.DiscountAmount.Amount.ShouldEqual(4);
            price.AppliedDiscounts.FirstOrDefault().ShouldNotBeNull();
            price.AppliedDiscounts.FirstOrDefault().ShouldEqual(discount2);
        }

        [Test]
        public async Task Ensure_discount_is_not_applied_to_products_with_prices_entered_by_customer()
        {
            _priceCalculationContext.Options.IgnoreDiscounts = false;
            _priceCalculationContext.Options.DiscountValidationFlags = DiscountValidationFlags.None;

            _product.CustomerEntersPrice = true;

            var discount1 = new Discount()
            {
                Id = 1,
                Name = "Discount 1",
                DiscountType = DiscountType.AssignedToSkus,
                DiscountAmount = 3,
                DiscountLimitation = DiscountLimitationType.Unlimited
            };
            discount1.AppliedToProducts.Add(_product);
            _product.AppliedDiscounts.Add(discount1);
            _product.HasDiscountsApplied = true;

            DbContext.Discounts.Add(discount1);
            DbContext.Products.Add(_product);
            await DbContext.SaveChangesAsync();

            var price = await _priceCalcService.CalculatePriceAsync(_priceCalculationContext);

            price.DiscountAmount.Amount.ShouldEqual(0);
            price.AppliedDiscounts.FirstOrDefault().ShouldBeNull();
        }

        [Test]
        public async Task Can_get_shopping_cart_item_unitPrice()
        {
            var item = new OrganizedShoppingCartItem(_sci);
            var calculationOptions = _priceCalcService.CreateDefaultOptions(false, _customer, _currency, _productBatchContext);
            var calculationContext = await _priceCalcService.CreateCalculationContextAsync(item, calculationOptions);
            var (unitPrice, _) = await _priceCalcService.CalculateSubtotalAsync(calculationContext);

            unitPrice.FinalPrice.Amount.ShouldEqual(12.34);
        }

        [Test]
        public async Task Can_get_shopping_cart_item_subTotal()
        {
            var item = new OrganizedShoppingCartItem(_sci);
            var calculationOptions = _priceCalcService.CreateDefaultOptions(false, _customer, _currency, _productBatchContext);
            var calculationContext = await _priceCalcService.CreateCalculationContextAsync(item, calculationOptions);
            var (_, subtotal) = await _priceCalcService.CalculateSubtotalAsync(calculationContext);

            subtotal.FinalPrice.Amount.ShouldEqual(24.68);
        }
    }
}