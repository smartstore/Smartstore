using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Engine;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Catalog.Pricing
{
    [TestFixture]
    public class PriceCalculationServiceTests : ServiceTest
    {
        IPriceCalculationService _priceCalcService;

        IStoreContext _storeContext;
        IWorkContext _workContext;
        IPriceCalculatorFactory _priceCalculatorFactory;
        ITaxCalculator _taxCalculator;
        IProductAttributeMaterializer _productAttributeMaterializer;
        IProductService _productService;
        ICategoryService _categoryService;
        IManufacturerService _manufacturerService;

        ICurrencyService _currencyService;
        ICommonServices _services;
        ITaxService _taxService;

        CatalogSettings _catalogSettings;
        TaxSettings _taxSettings;
        Store _store;
        Currency _currency;
        Customer _customer;
        Language _language;
        Product _product;

        [OneTimeSetUp]
        public new void SetUp()
        {
            _store = new Store { Id = 1 };
            var storeContextWrapper = new Mock<IStoreContext>();
            _storeContext = storeContextWrapper.Object;
            storeContextWrapper.Setup(x => x.CurrentStore).Returns(_store);

            _currency = new Currency { Id = 1 };
            _customer = new Customer();
            _language = new Language();
            _product = new Product
            {
                Id = 1,
                Name = "Product name 1",
                Price = 12.34M,
                CustomerEntersPrice = false,
                Published = true,
                ProductType = ProductType.SimpleProduct
            };

            var workContextWrapper = new Mock<IWorkContext>();
            _workContext = workContextWrapper.Object;
            workContextWrapper.Setup(x => x.WorkingCurrency).Returns(_currency);

            var productServiceWrapper = new Mock<IProductService>();
            _productService = productServiceWrapper.Object;

            var categoryServiceWrapper = new Mock<ICategoryService>();
            _categoryService = categoryServiceWrapper.Object;

            var manufacturerServiceWrapper = new Mock<IManufacturerService>();
            _manufacturerService = manufacturerServiceWrapper.Object;

            var commonServicesWrapper = new Mock<ICommonServices>();
            _services = commonServicesWrapper.Object;
            commonServicesWrapper.Setup(x => x.StoreContext).Returns(_storeContext);
            commonServicesWrapper.Setup(x => x.WorkContext).Returns(_workContext);

            var builder = new ContainerBuilder();
            builder.RegisterInstance(_productService).As<IProductService>().SingleInstance();
            builder.RegisterInstance(_categoryService).As<ICategoryService>().SingleInstance();
            builder.RegisterInstance(_manufacturerService).As<IManufacturerService>().SingleInstance();

            commonServicesWrapper.Setup(x => x.Container).Returns(builder.Build());

            var productAttributeMaterializerWrapper = new Mock<IProductAttributeMaterializer>();
            _productAttributeMaterializer = productAttributeMaterializerWrapper.Object;

            var taxServiceWrapper = new Mock<ITaxService>();
            _taxService = taxServiceWrapper.Object;

            var currencyServiceWrapper = new Mock<ICurrencyService>();
            _currencyService = currencyServiceWrapper.Object;
            currencyServiceWrapper.Setup(x => x.PrimaryCurrency).Returns(_currency);
            currencyServiceWrapper.Setup(x => x.PrimaryExchangeCurrency).Returns(_currency);

            _catalogSettings = new CatalogSettings();
            _taxSettings = new TaxSettings();

            // INFO: no mocking here to use real implementation.
            _taxCalculator = new TaxCalculator(DbContext, _workContext, _taxService, _taxSettings);


            // TODO: (mh) (core) Somehow a mocked version of DbContext mus be registered so it can be resolved by autofac :-/
            _priceCalculatorFactory = Engine.ResolveService<IPriceCalculatorFactory>();

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
                _catalogSettings,
                _taxSettings);
        }

        [Test]
        public async Task Can_get_final_product_price()
        {
            // TODO: (mh) (core) Place in setup
            var batchContext = new ProductBatchContext(new List<Product>{ _product }, _services, _store, _customer, false);
            var ctx = new PriceCalculationContext(_product, new PriceCalculationOptions(batchContext, _customer, _store, _language, _currency) { IgnoreDiscounts = true })
            {
                Quantity = 1, 
                AdditionalCharge = 0
            };

            var price = await _priceCalcService.CalculatePriceAsync(ctx);
            price.FinalPrice.Amount.ShouldEqual(12.34M);

            ctx.Quantity = 2;
            price = await _priceCalcService.CalculatePriceAsync(ctx);

            price.FinalPrice.Amount.ShouldEqual(12.34M);
        }

        [Test]
        public async Task Can_get_final_product_price_with_tier_prices()
        {
            //add tier prices
            _product.TierPrices.Add(new TierPrice
            {
                Price = 10,
                Quantity = 2,
                Product = _product,
                CalculationMethod = TierPriceCalculationMethod.Fixed
            });
            _product.TierPrices.Add(new TierPrice
            {
                Price = 8,
                Quantity = 5,
                Product = _product,
                CalculationMethod = TierPriceCalculationMethod.Fixed
            });

            _product.TierPrices.Add(new TierPrice
            {
                Price = 1,
                Quantity = 10,
                Product = _product,
                CalculationMethod = TierPriceCalculationMethod.Adjustment
            });

            _product.TierPrices.Add(new TierPrice
            {
                Price = 50,
                Quantity = 20,
                Product = _product,
                CalculationMethod = TierPriceCalculationMethod.Percental
            });

            // set HasTierPrices property
            _product.HasTierPrices = true;

            // customer
            var batchContext = new ProductBatchContext(new List<Product> { _product }, _services, _store, _customer, false);
            var ctx = new PriceCalculationContext(_product, new PriceCalculationOptions(batchContext, _customer, _store, _language, _currency) 
            { 
                IgnoreDiscounts = true, 
                IgnoreTierPrices = false 
            })
            {
                Quantity = 1,
                AdditionalCharge = 0
            };

            var price = await _priceCalcService.CalculatePriceAsync(ctx);
            price.FinalPrice.Amount.ShouldEqual(12.34M);

            ctx.Quantity = 2;

            price = await _priceCalcService.CalculatePriceAsync(ctx);
            price.FinalPrice.Amount.ShouldEqual(10);

            //_priceCalcService.GetFinalPrice(product, customer, 0, false, 1, isTierPrice: true).ShouldEqual(12.34M);
            //_priceCalcService.GetFinalPrice(product, customer, 0, false, 2, isTierPrice: true).ShouldEqual(10);
            //_priceCalcService.GetFinalPrice(product, customer, 0, false, 3, isTierPrice: true).ShouldEqual(10);
            //_priceCalcService.GetFinalPrice(product, customer, 0, false, 5, isTierPrice: true).ShouldEqual(8);
            //_priceCalcService.GetFinalPrice(product, customer, 0, false, 10, isTierPrice: true).ShouldEqual(11.34M);
            //_priceCalcService.GetFinalPrice(product, customer, 0, false, 20, isTierPrice: true).ShouldEqual(6.17M);
        }

        [Test]
        public void Can_get_final_product_price_with_tier_prices_by_customerRole()
        {
            var product = new Product
            {
                Id = 1,
                Name = "Product name 1",
                Price = 12.34M,
                CustomerEntersPrice = false,
                Published = true,
            };

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

            product.TierPrices.Add(new TierPrice
            {
                Price = 10,
                Quantity = 2,
                Product = product,
                CustomerRole = customerRole1,
                CalculationMethod = TierPriceCalculationMethod.Fixed
            });
            product.TierPrices.Add(new TierPrice
            {
                Price = 9,
                Quantity = 2,
                Product = product,
                CustomerRole = customerRole2,
                CalculationMethod = TierPriceCalculationMethod.Fixed
            });
            product.TierPrices.Add(new TierPrice
            {
                Price = 8,
                Quantity = 5,
                Product = product,
                CustomerRole = customerRole1,
                CalculationMethod = TierPriceCalculationMethod.Fixed
            });
            product.TierPrices.Add(new TierPrice
            {
                Price = 5,
                Quantity = 10,
                Product = product,
                CustomerRole = customerRole2,
                CalculationMethod = TierPriceCalculationMethod.Fixed
            });
            product.HasTierPrices = true;

            var customer = new Customer();
            customer.CustomerRoleMappings.Add(new CustomerRoleMapping
            {
                CustomerId = customer.Id,
                CustomerRoleId = customerRole1.Id,
                CustomerRole = customerRole1
            });

            //_priceCalcService.GetFinalPrice(product, customer, 0, false, 1, isTierPrice: true).ShouldEqual(12.34M);
            //_priceCalcService.GetFinalPrice(product, customer, 0, false, 2, isTierPrice: true).ShouldEqual(10);
            //_priceCalcService.GetFinalPrice(product, customer, 0, false, 3, isTierPrice: true).ShouldEqual(10);
            //_priceCalcService.GetFinalPrice(product, customer, 0, false, 5, isTierPrice: true).ShouldEqual(8);
            //_priceCalcService.GetFinalPrice(product, customer, 0, false, 10, isTierPrice: true).ShouldEqual(8);
        }

        [Test]
        public async Task Can_get_final_product_price_with_additionalFee()
        {
            // TODO: (mh) (core) Place in setup
            var batchContext = new ProductBatchContext(new List<Product> { _product }, _services, _store, _customer, false);
            var ctx = new PriceCalculationContext(_product, new PriceCalculationOptions(batchContext, _customer, _store, _language, _currency) { IgnoreDiscounts = true })
            {
                Quantity = 1,
                AdditionalCharge = 0
            };

            ctx.AdditionalCharge = 5;

            var price = await _priceCalcService.CalculatePriceAsync(ctx);
            price.FinalPrice.Amount.ShouldEqual(17.34M);
        }

        [Test]
        public void Can_get_final_product_price_with_discount()
        {
            var product = new Product
            {
                Id = 1,
                Name = "Product name 1",
                Price = 12.34M,
                CustomerEntersPrice = false,
                Published = true,
            };

            //customer
            Customer customer = null;

            //discounts
            var discount1 = new Discount()
            {
                Id = 1,
                Name = "Discount 1",
                DiscountType = DiscountType.AssignedToSkus,
                DiscountAmount = 3,
                DiscountLimitation = DiscountLimitationType.Unlimited
            };
            discount1.AppliedToProducts.Add(product);
            product.AppliedDiscounts.Add(discount1);
            //set HasDiscountsApplied property
            product.HasDiscountsApplied = true;
            //_discountService.Expect(ds => ds.IsDiscountValid(discount1, customer)).Return(true);
            //_discountService.Expect(ds => ds.GetAllDiscounts(DiscountType.AssignedToCategories)).Return(new List<Discount>());
            //_discountService.Expect(ds => ds.GetAllDiscounts(DiscountType.AssignedToManufacturers)).Return(new List<Discount>());

            //_priceCalcService.GetFinalPrice(product, customer, 0, true, 1).ShouldEqual(9.34M);
        }

        [Test]
        public void Can_get_final_product_price_with_special_price()
        {
            var product = new Product
            {
                Id = 1,
                Name = "Product name 1",
                Price = 12.34M,
                SpecialPrice = 10.01M,
                SpecialPriceStartDateTimeUtc = DateTime.UtcNow.AddDays(-1),
                SpecialPriceEndDateTimeUtc = DateTime.UtcNow.AddDays(1),
                CustomerEntersPrice = false,
                Published = true,
            };

            //_discountService.Expect(ds => ds.GetAllDiscounts(DiscountType.AssignedToCategories)).Return(new List<Discount>());
            //_discountService.Expect(ds => ds.GetAllDiscounts(DiscountType.AssignedToManufacturers)).Return(new List<Discount>());

            ////customer
            //Customer customer = null;
            ////valid dates
            //_priceCalcService.GetFinalPrice(product, customer, 0, true, 1).ShouldEqual(10.01M);

            ////invalid date
            //product.SpecialPriceStartDateTimeUtc = DateTime.UtcNow.AddDays(1);
            //_priceCalcService.GetFinalPrice(product, customer, 0, true, 1).ShouldEqual(12.34M);

            ////no dates
            //product.SpecialPriceStartDateTimeUtc = null;
            //product.SpecialPriceEndDateTimeUtc = null;
            //_priceCalcService.GetFinalPrice(product, customer, 0, true, 1).ShouldEqual(10.01M);
        }

        [Test]
        public void Can_get_final_product_price_with_variant_combination_price()
        {
            var product = new Product
            {
                Id = 1,
                Name = "Product name 1",
                Price = 9.99M,
                CustomerEntersPrice = false,
                Published = true,
            };

            var combination = new ProductVariantAttributeCombination
            {
                Id = 1,
                Price = 18.90M,
                ProductId = 1
            };

            product.MergeWithCombination(combination);

            //_discountService.Expect(ds => ds.GetAllDiscounts(DiscountType.AssignedToCategories)).Return(new List<Discount>());
            //_discountService.Expect(ds => ds.GetAllDiscounts(DiscountType.AssignedToManufacturers)).Return(new List<Discount>());

            //_priceCalcService.GetFinalPrice(product, null, 0, true, 1).ShouldEqual(18.90M);
        }

        //[Test]
        //public void Can_get_product_discount()
        //{
        //    var product = new Product
        //    {
        //        Id = 1,
        //        Name = "Product name 1",
        //        Price = 12.34M,
        //        CustomerEntersPrice = false,
        //        Published = true,
        //    };

        //    //customer
        //    Customer customer = null;

        //    //discounts
        //    var discount1 = new Discount()
        //    {
        //        Id = 1,
        //        Name = "Discount 1",
        //        DiscountType = DiscountType.AssignedToSkus,
        //        DiscountAmount = 3,
        //        DiscountLimitation = DiscountLimitationType.Unlimited
        //    };
        //    discount1.AppliedToProducts.Add(product);
        //    product.AppliedDiscounts.Add(discount1);
        //    //set HasDiscountsApplied property
        //    product.HasDiscountsApplied = true;
        //    _discountService.Expect(ds => ds.IsDiscountValid(discount1, customer)).Return(true);
        //    _discountService.Expect(ds => ds.GetAllDiscounts(DiscountType.AssignedToCategories)).Return(new List<Discount>());
        //    _discountService.Expect(ds => ds.GetAllDiscounts(DiscountType.AssignedToManufacturers)).Return(new List<Discount>());

        //    var discount2 = new Discount()
        //    {
        //        Id = 2,
        //        Name = "Discount 2",
        //        DiscountType = DiscountType.AssignedToSkus,
        //        DiscountAmount = 4,
        //        DiscountLimitation = DiscountLimitationType.Unlimited
        //    };
        //    discount2.AppliedToProducts.Add(product);
        //    product.AppliedDiscounts.Add(discount2);
        //    _discountService.Expect(ds => ds.IsDiscountValid(discount2, customer)).Return(true);

        //    var discount3 = new Discount()
        //    {
        //        Id = 3,
        //        Name = "Discount 3",
        //        DiscountType = DiscountType.AssignedToOrderSubTotal,
        //        DiscountAmount = 5,
        //        DiscountLimitation = DiscountLimitationType.Unlimited,
        //        RequiresCouponCode = true,
        //        CouponCode = "SECRET CODE"
        //    };
        //    discount3.AppliedToProducts.Add(product);
        //    product.AppliedDiscounts.Add(discount3);
        //    //discount is not valid
        //    _discountService.Expect(ds => ds.IsDiscountValid(discount3, customer)).Return(false);


        //    Discount appliedDiscount;
        //    _priceCalcService.GetDiscountAmount(product, customer, 0, 1, out appliedDiscount).ShouldEqual(4);
        //    appliedDiscount.ShouldNotBeNull();
        //    appliedDiscount.ShouldEqual(discount2);
        //}

        //[Test]
        //public void Ensure_discount_is_not_applied_to_products_with_prices_entered_by_customer()
        //{
        //    var product = new Product
        //    {
        //        Id = 1,
        //        Name = "Product name 1",
        //        Price = 12.34M,
        //        CustomerEntersPrice = true,
        //        Published = true,
        //    };

        //    //customer
        //    Customer customer = null;

        //    //discounts
        //    var discount1 = new Discount()
        //    {
        //        Id = 1,
        //        Name = "Discount 1",
        //        DiscountType = DiscountType.AssignedToSkus,
        //        DiscountAmount = 3,
        //        DiscountLimitation = DiscountLimitationType.Unlimited
        //    };
        //    discount1.AppliedToProducts.Add(product);
        //    product.AppliedDiscounts.Add(discount1);
        //    _discountService.Expect(ds => ds.IsDiscountValid(discount1, customer)).Return(true);

        //    Discount appliedDiscount;
        //    _priceCalcService.GetDiscountAmount(product, customer, 0, 1, out appliedDiscount).ShouldEqual(0);
        //    appliedDiscount.ShouldBeNull();
        //}

        //[Test]
        //public void Can_get_shopping_cart_item_unitPrice()
        //{
        //    //customer
        //    var customer = new Customer();

        //    //shopping cart
        //    var product1 = new Product
        //    {
        //        Id = 1,
        //        Name = "Product name 1",
        //        Price = 12.34M,
        //        CustomerEntersPrice = false,
        //        Published = true,
        //    };
        //    var sci1 = new ShoppingCartItem()
        //    {
        //        Customer = customer,
        //        CustomerId = customer.Id,
        //        Product = product1,
        //        ProductId = product1.Id,
        //        Quantity = 2,
        //    };

        //    var item = new OrganizedShoppingCartItem(sci1);

        //    _priceCalcService.GetUnitPrice(item, false).ShouldEqual(12.34);
        //}

        //[Test]
        //public void Can_get_shopping_cart_item_subTotal()
        //{
        //    //customer
        //    var customer = new Customer();

        //    //shopping cart
        //    var product1 = new Product
        //    {
        //        Id = 1,
        //        Name = "Product name 1",
        //        Price = 12.34M,
        //        CustomerEntersPrice = false,
        //        Published = true,
        //    };
        //    var sci1 = new ShoppingCartItem()
        //    {
        //        Customer = customer,
        //        CustomerId = customer.Id,
        //        Product = product1,
        //        ProductId = product1.Id,
        //        Quantity = 2,
        //    };

        //    var item = new OrganizedShoppingCartItem(sci1);

        //    _priceCalcService.GetSubTotal(item, false).ShouldEqual(24.68);
        //}

        private void DiscoverCalculators(ContainerBuilder builder, IApplicationContext appContext)
        {
            var calculatorTypes = appContext.TypeScanner.FindTypes<IPriceCalculator>();

            foreach (var calculatorType in calculatorTypes)
            {
                var usageAttribute = calculatorType.GetAttribute<CalculatorUsageAttribute>(true);

                var registration = builder
                    .RegisterType(calculatorType)
                    .As<IPriceCalculator>()
                    .Keyed<IPriceCalculator>(calculatorType)
                    .WithMetadata<PriceCalculatorMetadata>(m =>
                    {
                        m.For(em => em.CalculatorType, calculatorType);
                        m.For(em => em.ValidTargets, usageAttribute?.ValidTargets ?? CalculatorTargets.All);
                        m.For(em => em.Order, usageAttribute?.Order ?? CalculatorOrdering.Default);
                    });

                var lifetime = calculatorType.GetAttribute<ServiceLifetimeAttribute>(false)?.Lifetime ?? ServiceLifetime.Scoped;

                if (lifetime == ServiceLifetime.Singleton)
                {
                    registration.SingleInstance();
                }
                else if (lifetime == ServiceLifetime.Transient)
                {
                    registration.InstancePerDependency();
                }
                else
                {
                    registration.InstancePerLifetimeScope();
                }
            }

            // Register calculator resolve delegate
            builder.Register<Func<Type, IPriceCalculator>>(c =>
            {
                var cc = c.Resolve<IComponentContext>();
                return key => cc.ResolveKeyed<IPriceCalculator>(key);
            });
        }
    }
}
