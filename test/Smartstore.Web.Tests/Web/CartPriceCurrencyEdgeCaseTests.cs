using System;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Engine;
using Smartstore.Web.Models.Cart;

namespace Smartstore.Web.Tests.Web;

[TestFixture, NonParallelizable]
public class CartPriceCurrencyEdgeCaseTests
{
    [TestCase(0.5)]
    [TestCase(1)]
    [TestCase(1.5)]
    [TestCase(2)]
    public async Task Shopping_cart_converts_primary_currency_once_and_preserves_tax_and_savings(decimal rate)
    {
        using var fixture = new CurrencyMappingFixture(rate);
        var primary = fixture.Primary;
        var model = await fixture.MapAsync(
            ShoppingCartType.ShoppingCart,
            new Money(25m, primary),
            new Money(75m, primary),
            new Money(6m, primary),
            new PriceSaving
            {
                HasSaving = true,
                SavingPrice = new Money(30m, primary),
                SavingAmount = new Money(5m, primary),
                SavingPercent = 16.67f
            },
            taxFormat: "{0} incl. tax",
            shippingCharge: 3.75m,
            quantity: 3);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model.Price.UnitPrice.Amount, Is.EqualTo(25m * rate));
            Assert.That(model.Price.SubTotal.Amount, Is.EqualTo(75m * rate));
            Assert.That(model.Price.TotalDiscount.Amount, Is.EqualTo(6m * rate));
            Assert.That(model.Price.Saving.SavingAmount?.Amount, Is.EqualTo(5m * rate));
            Assert.That(model.Price.Saving.SavingPrice.Amount, Is.EqualTo(30m * rate));
            Assert.That(model.Price.Saving.SavingPercent, Is.EqualTo(16.67f));
            Assert.That(model.Price.ShippingSurcharge?.Amount, Is.EqualTo(3.75m * rate));
            Assert.That(model.Price.UnitPrice.Currency, Is.SameAs(fixture.Working));
            Assert.That(model.Price.Saving.SavingPrice.Currency, Is.SameAs(fixture.Working));
            Assert.That(model.Price.ShippingSurcharge?.Currency, Is.SameAs(fixture.Working));
            Assert.That(model.Price.UnitPrice.PostFormat, Is.EqualTo("{0} incl. tax"));
            Assert.That(model.Price.SubTotal.PostFormat, Is.EqualTo("{0} incl. tax"));
            Assert.That(model.Price.TotalDiscount.PostFormat, Is.EqualTo("{0} incl. tax"));
            Assert.That(model.Price.Quantity, Is.EqualTo(3));
        }
    }

    [TestCase(0.5)]
    [TestCase(1.5)]
    [TestCase(2)]
    public async Task Shopping_cart_does_not_reconvert_money_already_in_working_currency(decimal rate)
    {
        using var fixture = new CurrencyMappingFixture(rate);
        var currency = fixture.Working;
        var model = await fixture.MapAsync(
            ShoppingCartType.ShoppingCart,
            new Money(15.25m, currency),
            new Money(30.50m, currency),
            new Money(1.25m, currency));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model.Price.UnitPrice.Amount, Is.EqualTo(15.25m));
            Assert.That(model.Price.SubTotal.Amount, Is.EqualTo(30.50m));
            Assert.That(model.Price.TotalDiscount.Amount, Is.EqualTo(1.25m));
            Assert.That(model.Price.UnitPrice.Currency, Is.SameAs(currency));
        }
    }

    [TestCase(1.5)]
    [TestCase(2)]
    public async Task Bundle_subitem_converts_prices_from_primary_currency(decimal rate)
    {
        using var fixture = new CurrencyMappingFixture(rate);
        var model = await fixture.MapAsync(
            ShoppingCartType.Wishlist,
            new Money(12.50m, fixture.Primary),
            new Money(25m, fixture.Primary),
            new Money(2m, fixture.Primary),
            bundleSubItem: true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model.Price.UnitPrice.Amount, Is.EqualTo(12.50m * rate));
            Assert.That(model.Price.SubTotal.Amount, Is.EqualTo(25m * rate));
            Assert.That(model.Price.TotalDiscount.Amount, Is.EqualTo(2m * rate));
            Assert.That(model.Price.UnitPrice.Currency, Is.SameAs(fixture.Working));
            Assert.That(model.Price.IsBundlePart, Is.True);
        }
    }

    [TestCase(0.5)]
    [TestCase(2)]
    public async Task Wishlist_converts_money_when_calculator_returns_primary_currency(decimal rate)
    {
        using var fixture = new CurrencyMappingFixture(rate);
        var model = await fixture.MapAsync(
            ShoppingCartType.Wishlist,
            new Money(17.35m, fixture.Primary),
            new Money(34.70m, fixture.Primary),
            new Money(2m, fixture.Primary));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model.Price.UnitPrice.Amount, Is.EqualTo(17.35m * rate));
            Assert.That(model.Price.SubTotal.Amount, Is.EqualTo(34.70m * rate));
            Assert.That(model.Price.TotalDiscount.Amount, Is.EqualTo(2m * rate));
        }
    }

    [TestCase(1)]
    [TestCase(2)]
    public async Task Call_for_price_preserves_the_original_money(decimal rate)
    {
        using var fixture = new CurrencyMappingFixture(rate);
        var model = await fixture.MapAsync(
            ShoppingCartType.Wishlist,
            new Money(0, fixture.Working).WithPostFormat("Call for price"),
            new Money(30, fixture.Working),
            new Money(0, fixture.Working),
            callForPrice: true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model.Price.UnitPrice.Amount, Is.Zero);
            Assert.That(model.Price.SubTotal.Amount, Is.Zero);
            Assert.That(model.Price.UnitPrice.PostFormat, Is.EqualTo("Call for price"));
            Assert.That(model.Price.SubTotal.PostFormat, Is.EqualTo("Call for price"));
            Assert.That(model.Price.UnitPrice.Currency, Is.SameAs(fixture.Working));
        }
    }

    [TestCase(0.5)]
    [TestCase(2)]
    public async Task Zero_value_changes_currency_without_changing_amount(decimal rate)
    {
        using var fixture = new CurrencyMappingFixture(rate);
        var model = await fixture.MapAsync(
            ShoppingCartType.Wishlist,
            new Money(0, fixture.Primary),
            new Money(0, fixture.Primary),
            new Money(0, fixture.Primary));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model.Price.UnitPrice.Amount, Is.Zero);
            Assert.That(model.Price.SubTotal.Amount, Is.Zero);
            Assert.That(model.Price.UnitPrice.Currency, Is.SameAs(fixture.Working));
            Assert.That(model.Price.SubTotal.Currency, Is.SameAs(fixture.Working));
        }
    }

    [Test]
    public async Task Exchange_between_two_non_primary_currencies_respects_source_rate()
    {
        using var fixture = new CurrencyMappingFixture(3m);
        var euro = new Currency { Id = 3, CurrencyCode = "EUR", Rate = 2m };
        var model = await fixture.MapAsync(
            ShoppingCartType.Wishlist,
            new Money(20m, euro),
            new Money(40m, euro),
            new Money(4m, euro));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model.Price.UnitPrice.Amount, Is.EqualTo(30m));
            Assert.That(model.Price.SubTotal.Amount, Is.EqualTo(60m));
            Assert.That(model.Price.TotalDiscount.Amount, Is.EqualTo(6m));
            Assert.That(model.Price.UnitPrice.Currency, Is.SameAs(fixture.Working));
        }
    }

    [Test]
    public async Task Non_primary_exchange_currency_is_respected()
    {
        using var fixture = new CurrencyMappingFixture(
            workingRate: 5m,
            primaryRate: 2m,
            exchangeCurrency: new Currency { Id = 3, CurrencyCode = "EUR", Rate = 1m });
        var model = await fixture.MapAsync(
            ShoppingCartType.Wishlist,
            new Money(20m, fixture.Primary),
            new Money(40m, fixture.Primary),
            new Money(0, fixture.Primary));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model.Price.UnitPrice.Amount, Is.EqualTo(50m));
            Assert.That(model.Price.SubTotal.Amount, Is.EqualTo(100m));
            Assert.That(model.Price.UnitPrice.Currency, Is.SameAs(fixture.Working));
        }
    }

    [Test]
    public async Task Already_converted_fractional_currency_values_remain_exact()
    {
        using var fixture = new CurrencyMappingFixture(1.2345m);
        var model = await fixture.MapAsync(
            ShoppingCartType.Wishlist,
            new Money(12.34567m, fixture.Working),
            new Money(61.72835m, fixture.Working),
            new Money(0.12345m, fixture.Working),
            quantity: 5);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model.Price.UnitPrice.Amount, Is.EqualTo(12.34567m));
            Assert.That(model.Price.SubTotal.Amount, Is.EqualTo(61.72835m));
            Assert.That(model.Price.TotalDiscount.Amount, Is.EqualTo(0.12345m));
            Assert.That(model.Price.Quantity, Is.EqualTo(5));
        }
    }

    [TestCase(0.5)]
    [TestCase(2)]
    public async Task Regular_and_retail_prices_keep_their_calculated_currency(decimal rate)
    {
        using var fixture = new CurrencyMappingFixture(rate);
        var currency = fixture.Working;
        var model = await fixture.MapAsync(
            ShoppingCartType.Wishlist,
            new Money(20m * rate, currency),
            new Money(40m * rate, currency),
            new Money(2m * rate, currency),
            new PriceSaving
            {
                HasSaving = true,
                SavingPrice = new Money(25m * rate, currency),
                SavingAmount = new Money(5m * rate, currency)
            },
            regularPrice: new Money(25m * rate, currency),
            retailPrice: new Money(30m * rate, currency));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model.Price.RegularPrice, Is.Not.Null);
            Assert.That(model.Price.RetailPrice, Is.Not.Null);
            Assert.That(model.Price.RegularPrice.Price.Amount, Is.EqualTo(25m * rate));
            Assert.That(model.Price.RetailPrice.Price.Amount, Is.EqualTo(30m * rate));
            Assert.That(model.Price.RegularPrice.Price.Currency, Is.SameAs(currency));
            Assert.That(model.Price.RetailPrice.Price.Currency, Is.SameAs(currency));
        }
    }

    [TestCase(0.5)]
    [TestCase(2)]
    public async Task Retail_price_is_converted_when_no_saving_is_present(decimal rate)
    {
        using var fixture = new CurrencyMappingFixture(rate);
        var model = await fixture.MapAsync(
            ShoppingCartType.Wishlist,
            new Money(15m, fixture.Primary),
            new Money(30m, fixture.Primary),
            new Money(0m, fixture.Primary),
            retailPrice: new Money(22m, fixture.Primary));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model.Price.RetailPrice, Is.Not.Null);
            Assert.That(model.Price.RetailPrice.Price.Amount, Is.EqualTo(22m * rate));
            Assert.That(model.Price.RetailPrice.Price.Currency, Is.SameAs(fixture.Working));
        }
    }

    [TestCase(ShoppingCartType.ShoppingCart)]
    [TestCase(ShoppingCartType.Wishlist)]
    public async Task Bundle_subitem_does_not_double_convert_target_currency(ShoppingCartType cartType)
    {
        using var fixture = new CurrencyMappingFixture(2m);
        var model = await fixture.MapAsync(
            cartType,
            new Money(19.25m, fixture.Working),
            new Money(38.50m, fixture.Working),
            new Money(4m, fixture.Working),
            bundleSubItem: true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model.Price.UnitPrice.Amount, Is.EqualTo(19.25m));
            Assert.That(model.Price.SubTotal.Amount, Is.EqualTo(38.50m));
            Assert.That(model.Price.TotalDiscount.Amount, Is.EqualTo(4m));
            Assert.That(model.Price.UnitPrice.Currency, Is.SameAs(fixture.Working));
            Assert.That(model.Price.IsBundlePart, Is.True);
        }
    }


    // Compare cart and Wishlist via their respective pricing paths rather than
    // checking only a single isolated conversion result.
    [TestCase(0.01)]
    [TestCase(0.1)]
    [TestCase(0.25)]
    [TestCase(0.75)]
    [TestCase(1.01)]
    [TestCase(1.2345)]
    [TestCase(2.5)]
    [TestCase(4)]
    [TestCase(10)]
    [TestCase(100)]
    public async Task Cart_primary_money_and_wishlist_preconverted_money_produce_identical_price_models(decimal rate)
    {
        using var fixture = new CurrencyMappingFixture(rate);
        var primary = fixture.Primary;
        var working = fixture.Working;
        var cart = await fixture.MapAsync(
            ShoppingCartType.ShoppingCart,
            new Money(17.25m, primary),
            new Money(51.75m, primary),
            new Money(2.50m, primary),
            new PriceSaving
            {
                HasSaving = true,
                SavingPrice = new Money(20m, primary),
                SavingAmount = new Money(2.75m, primary),
                SavingPercent = 13.75f
            },
            taxFormat: "{0} incl. VAT",
            quantity: 3);

        var wishlist = await fixture.MapAsync(
            ShoppingCartType.Wishlist,
            new Money(17.25m * rate, working),
            new Money(51.75m * rate, working),
            new Money(2.50m * rate, working),
            new PriceSaving
            {
                HasSaving = true,
                SavingPrice = new Money(20m * rate, working),
                SavingAmount = new Money(2.75m * rate, working),
                SavingPercent = 13.75f
            },
            taxFormat: "{0} incl. VAT",
            quantity: 3);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(cart.Price.UnitPrice.Amount, Is.EqualTo(wishlist.Price.UnitPrice.Amount));
            Assert.That(cart.Price.SubTotal.Amount, Is.EqualTo(wishlist.Price.SubTotal.Amount));
            Assert.That(cart.Price.TotalDiscount.Amount, Is.EqualTo(wishlist.Price.TotalDiscount.Amount));
            Assert.That(cart.Price.Saving.SavingAmount?.Amount, Is.EqualTo(wishlist.Price.Saving.SavingAmount?.Amount));
            Assert.That(cart.Price.Saving.SavingPrice.Amount, Is.EqualTo(wishlist.Price.Saving.SavingPrice.Amount));
            Assert.That(cart.Price.Saving.SavingPercent, Is.EqualTo(wishlist.Price.Saving.SavingPercent));
            Assert.That(wishlist.Price.UnitPrice.Amount, Is.EqualTo(17.25m * rate));
            Assert.That(wishlist.Price.SubTotal.Amount, Is.EqualTo(51.75m * rate));
            Assert.That(wishlist.Price.Quantity, Is.EqualTo(3));
            Assert.That(cart.Price.UnitPrice.Currency, Is.SameAs(working));
            Assert.That(wishlist.Price.UnitPrice.Currency, Is.SameAs(working));
            Assert.That(cart.Price.SubTotal.Currency, Is.SameAs(working));
            Assert.That(wishlist.Price.SubTotal.Currency, Is.SameAs(working));
            Assert.That(cart.Price.UnitPrice.PostFormat, Is.EqualTo("{0} incl. VAT"));
            Assert.That(wishlist.Price.UnitPrice.PostFormat, Is.EqualTo("{0} incl. VAT"));
        }
    }

    [TestCase(0.2)]
    [TestCase(0.8)]
    [TestCase(1.2)]
    [TestCase(2)]
    [TestCase(4)]
    public async Task Cart_and_wishlist_are_equal_when_both_price_pipelines_return_primary_currency(decimal rate)
    {
        using var fixture = new CurrencyMappingFixture(rate);
        var primary = fixture.Primary;
        var cart = await fixture.MapAsync(
            ShoppingCartType.ShoppingCart,
            new Money(12.50m, primary),
            new Money(50m, primary),
            new Money(3.25m, primary),
            quantity: 4);
        var wishlist = await fixture.MapAsync(
            ShoppingCartType.Wishlist,
            new Money(12.50m, primary),
            new Money(50m, primary),
            new Money(3.25m, primary),
            quantity: 4);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(wishlist.Price.UnitPrice.Amount, Is.EqualTo(cart.Price.UnitPrice.Amount));
            Assert.That(wishlist.Price.SubTotal.Amount, Is.EqualTo(cart.Price.SubTotal.Amount));
            Assert.That(wishlist.Price.TotalDiscount.Amount, Is.EqualTo(cart.Price.TotalDiscount.Amount));
            Assert.That(wishlist.Price.UnitPrice.Amount, Is.EqualTo(12.50m * rate));
            Assert.That(wishlist.Price.SubTotal.Amount, Is.EqualTo(50m * rate));
            Assert.That(wishlist.Price.Quantity, Is.EqualTo(4));
            Assert.That(wishlist.Price.UnitPrice.Currency, Is.SameAs(fixture.Working));
        }
    }

    [TestCase(0.125)]
    [TestCase(0.5)]
    [TestCase(1.75)]
    [TestCase(3.75)]
    [TestCase(20)]
    public async Task Both_cart_types_preserve_existing_target_currency_amounts_without_reconversion(decimal rate)
    {
        using var fixture = new CurrencyMappingFixture(rate);
        foreach (var type in new[] { ShoppingCartType.ShoppingCart, ShoppingCartType.Wishlist })
        {
            var model = await fixture.MapAsync(
                type,
                new Money(123.45678m, fixture.Working),
                new Money(370.37034m, fixture.Working),
                new Money(12.34567m, fixture.Working),
                quantity: 3);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(model.Price.UnitPrice.Amount, Is.EqualTo(123.45678m));
                Assert.That(model.Price.SubTotal.Amount, Is.EqualTo(370.37034m));
                Assert.That(model.Price.TotalDiscount.Amount, Is.EqualTo(12.34567m));
                Assert.That(model.Price.UnitPrice.Currency, Is.SameAs(fixture.Working));
                Assert.That(model.Price.SubTotal.Currency, Is.SameAs(fixture.Working));
            }
        }
    }

    [TestCase(0.5, 2)]
    [TestCase(2, 0.5)]
    [TestCase(2, 3)]
    [TestCase(0.25, 1.25)]
    [TestCase(3, 12)]
    [TestCase(4, 1)]
    public async Task Cross_currency_repricing_uses_source_rate_not_primary_currency_rate(decimal sourceRate, decimal workingRate)
    {
        using var fixture = new CurrencyMappingFixture(workingRate);
        var source = new Currency { Id = 4, CurrencyCode = "CHF", Rate = sourceRate };
        var result = await fixture.MapAsync(
            ShoppingCartType.Wishlist,
            new Money(16m, source),
            new Money(48m, source),
            new Money(4m, source),
            quantity: 3);
        var factor = workingRate / sourceRate;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Price.UnitPrice.Amount, Is.EqualTo(16m * factor));
            Assert.That(result.Price.SubTotal.Amount, Is.EqualTo(48m * factor));
            Assert.That(result.Price.TotalDiscount.Amount, Is.EqualTo(4m * factor));
            Assert.That(result.Price.UnitPrice.Currency, Is.SameAs(fixture.Working));
            Assert.That(result.Price.Quantity, Is.EqualTo(3));
        }
    }

    [TestCase(0.25)]
    [TestCase(0.5)]
    [TestCase(1.5)]
    [TestCase(2)]
    [TestCase(4)]
    public async Task Each_monetary_field_converts_independently_based_on_its_actual_source_currency(decimal rate)
    {
        using var fixture = new CurrencyMappingFixture(rate);
        var result = await fixture.MapAsync(
            ShoppingCartType.Wishlist,
            new Money(80m * rate, fixture.Working),
            new Money(160m * rate, fixture.Working),
            new Money(5m, fixture.Primary),
            new PriceSaving
            {
                HasSaving = true,
                SavingPrice = new Money(90m, fixture.Primary),
                SavingAmount = new Money(10m * rate, fixture.Working),
                SavingPercent = 11.11f
            },
            taxFormat: "{0} including taxes");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Price.UnitPrice.Amount, Is.EqualTo(80m * rate));
            Assert.That(result.Price.SubTotal.Amount, Is.EqualTo(160m * rate));
            Assert.That(result.Price.TotalDiscount.Amount, Is.EqualTo(5m * rate));
            Assert.That(result.Price.Saving.SavingPrice.Amount, Is.EqualTo(90m * rate));
            Assert.That(result.Price.Saving.SavingAmount?.Amount, Is.EqualTo(10m * rate));
            Assert.That(result.Price.Saving.SavingPercent, Is.EqualTo(11.11f));
            Assert.That(result.Price.UnitPrice.PostFormat, Is.EqualTo("{0} including taxes"));
            Assert.That(result.Price.TotalDiscount.PostFormat, Is.EqualTo("{0} including taxes"));
        }
    }

    [TestCase(ShoppingCartType.ShoppingCart, 0.5)]
    [TestCase(ShoppingCartType.ShoppingCart, 2)]
    [TestCase(ShoppingCartType.Wishlist, 0.5)]
    [TestCase(ShoppingCartType.Wishlist, 2)]
    public async Task Bundle_subitems_are_consistent_with_equivalent_standalone_items(
        ShoppingCartType cartType, decimal rate)
    {
        using var fixture = new CurrencyMappingFixture(rate);
        var bundle = await fixture.MapAsync(
            cartType,
            new Money(11.50m * rate, fixture.Working),
            new Money(34.50m * rate, fixture.Working),
            new Money(2.25m * rate, fixture.Working),
            bundleSubItem: true,
            quantity: 3);
        var standalone = await fixture.MapAsync(
            cartType,
            new Money(11.50m * rate, fixture.Working),
            new Money(34.50m * rate, fixture.Working),
            new Money(2.25m * rate, fixture.Working),
            bundleSubItem: false,
            quantity: 3);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(bundle.Price.UnitPrice.Amount, Is.EqualTo(standalone.Price.UnitPrice.Amount));
            Assert.That(bundle.Price.SubTotal.Amount, Is.EqualTo(standalone.Price.SubTotal.Amount));
            Assert.That(bundle.Price.TotalDiscount.Amount, Is.EqualTo(standalone.Price.TotalDiscount.Amount));
            Assert.That(bundle.Price.UnitPrice.Currency, Is.SameAs(fixture.Working));
            Assert.That(bundle.Price.IsBundlePart, Is.True);
            Assert.That(standalone.Price.IsBundlePart, Is.False);
        }
    }

    [TestCase(0.01)]
    [TestCase(0.5)]
    [TestCase(1.75)]
    [TestCase(5)]
    public async Task Call_for_price_skips_numeric_subtotals_in_both_cart_types(decimal rate)
    {
        using var fixture = new CurrencyMappingFixture(rate);
        foreach (var type in new[] { ShoppingCartType.ShoppingCart, ShoppingCartType.Wishlist })
        {
            var model = await fixture.MapAsync(
                type,
                new Money(0m, fixture.Working).WithPostFormat("Contact us"),
                new Money(999m, fixture.Working),
                new Money(0m, fixture.Working),
                callForPrice: true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(model.Price.UnitPrice.Amount, Is.Zero);
                Assert.That(model.Price.SubTotal.Amount, Is.Zero);
                Assert.That(model.Price.UnitPrice.PostFormat, Is.EqualTo("Contact us"));
                Assert.That(model.Price.SubTotal.PostFormat, Is.EqualTo("Contact us"));
                Assert.That(model.Price.UnitPrice.Currency, Is.SameAs(fixture.Working));
            }
        }
    }

    [TestCase(0.125)]
    [TestCase(0.75)]
    [TestCase(1.5)]
    [TestCase(5)]
    public async Task Zero_discounts_and_shipping_charges_do_not_create_spurious_amounts(decimal rate)
    {
        using var fixture = new CurrencyMappingFixture(rate);
        var model = await fixture.MapAsync(
            ShoppingCartType.ShoppingCart,
            new Money(15m, fixture.Primary),
            new Money(30m, fixture.Primary),
            new Money(0m, fixture.Primary),
            shippingCharge: 0m);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model.Price.UnitPrice.Amount, Is.EqualTo(15m * rate));
            Assert.That(model.Price.SubTotal.Amount, Is.EqualTo(30m * rate));
            Assert.That(model.Price.TotalDiscount.Amount, Is.Zero);
            Assert.That(model.Price.ShippingSurcharge, Is.Null);
        }
    }

    private sealed class CurrencyMappingFixture : IDisposable
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly IEngine _previousEngine;
        private readonly IContainer _localizationContainer;

        public CurrencyMappingFixture(decimal workingRate, decimal primaryRate = 1m, Currency exchangeCurrency = null)
        {
            Primary = new Currency { Id = 1, CurrencyCode = "USD", Rate = primaryRate };
            Working = workingRate == primaryRate && exchangeCurrency == null
                ? Primary
                : new Currency { Id = 2, CurrencyCode = "GBP", Rate = workingRate };

            var currencies = new Mock<ICurrencyService>();
            currencies.SetupGet(x => x.PrimaryCurrency).Returns(Primary);
            currencies.SetupGet(x => x.PrimaryExchangeCurrency).Returns(exchangeCurrency ?? Primary);

            var workContext = new Mock<IWorkContext>();
            workContext.SetupGet(x => x.WorkingCurrency).Returns(Working);

            var services = new Mock<ICommonServices>();
            services.SetupGet(x => x.WorkContext).Returns(workContext.Object);
            services.SetupGet(x => x.CurrencyService).Returns(currencies.Object);
            _services = services.Object;

            _previousEngine = EngineContext.Current;
            _localizationContainer = new ContainerBuilder().Build();
            EngineContext.Replace(new SmartEngine
            {
                Scope = new ScopedServiceContainer(
                    Mock.Of<ILifetimeScopeAccessor>(), new HttpContextAccessor(), _localizationContainer)
            });

            _db = new SmartDbContext(new DbContextOptionsBuilder<SmartDbContext>().Options);
        }

        public Currency Primary { get; }
        public Currency Working { get; }

        public async Task<WishlistModel.WishlistItemModel> MapAsync(
            ShoppingCartType cartType,
            Money unitMoney,
            Money subtotalMoney,
            Money discountMoney,
            PriceSaving saving = default,
            string taxFormat = null,
            decimal shippingCharge = 0,
            int quantity = 2,
            bool bundleSubItem = false,
            bool callForPrice = false,
            Money? regularPrice = null,
            Money? retailPrice = null)
        {
            var product = new Product { Id = 11, AdditionalShippingCharge = shippingCharge };
            var customer = new Customer { Id = 13 };
            var item = new OrganizedShoppingCartItem(new ShoppingCartItem
            {
                Id = 15,
                Product = product,
                Customer = customer,
                Quantity = quantity,
                ShoppingCartType = cartType,
                BundleItem = bundleSubItem ? new ProductBundleItem { Product = product } : null
            });

            var unit = new CalculatedPrice(product, callForPrice ? PricingType.CallForPrice : PricingType.Calculated)
            {
                FinalPrice = unitMoney,
                Saving = saving,
                RegularPrice = regularPrice,
                RetailPrice = retailPrice,
                RegularPriceLabel = regularPrice.HasValue ? new PriceLabel { Name = "Regular", ShortName = "Reg" } : null,
                RetailPriceLabel = retailPrice.HasValue ? new PriceLabel { Name = "Retail", ShortName = "MSRP" } : null
            };
            var subtotal = new CalculatedPrice(product)
            {
                FinalPrice = subtotalMoney,
                DiscountAmount = discountMoney
            };

            var store = new Store();
            var batch = new ProductBatchContext(null, _db, Mock.Of<IComponentContext>(), store, customer, false);
            var options = new PriceCalculationOptions(batch, customer, store, new Language(), Working);
            var context = new PriceCalculationContext(product, quantity, options);

            var calculation = new Mock<IPriceCalculationService>();
            calculation.Setup(x => x.CreateDefaultOptions(false, customer, bundleSubItem ? null : Working, null))
                .Returns(options);
            calculation.Setup(x => x.CreateCalculationContextAsync(item, options)).ReturnsAsync(context);
            calculation.Setup(x => x.CalculateSubtotalAsync(context)).ReturnsAsync((unit, subtotal));

            var mapper = new TestMapper(_services, calculation.Object)
            {
                PriceSettings = new PriceSettings { ShowOfferBadge = false },
                PriceLabelService = Mock.Of<IPriceLabelService>()
            };
            var model = new WishlistModel.WishlistItemModel
            {
                EnteredQuantity = quantity,
                BundleItem = bundleSubItem
                    ? new BundleItemModel { PerItemPricing = true, PerItemShoppingCart = true }
                    : null
            };

            dynamic parameters = new GracefulDynamicObject();
            parameters.TaxFormat = taxFormat;
            if (cartType == ShoppingCartType.ShoppingCart && !bundleSubItem)
            {
                parameters.CartSubtotal = new ShoppingCartSubtotal
                {
                    LineItems = [new ShoppingCartLineItem(item) { UnitPrice = unit, Subtotal = subtotal }]
                };
            }

            await mapper.MapPriceOnlyAsync(item, model, (object)parameters);
            return model;
        }

        public void Dispose()
        {
            _db.Dispose();
            EngineContext.Replace(_previousEngine);
            _localizationContainer.Dispose();
        }
    }

    private sealed class TestMapper : WishlistItemModelMapper
    {
        public TestMapper(ICommonServices services, IPriceCalculationService priceCalculation)
            : base(services, priceCalculation, null, null, new ShoppingCartSettings(), new CatalogSettings(), null)
        {
        }

        public Task MapPriceOnlyAsync(OrganizedShoppingCartItem item, WishlistModel.WishlistItemModel model, object parameters)
            => MapPriceAsync(item, model, parameters);
    }
}
