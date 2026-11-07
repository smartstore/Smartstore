using System.Threading.Tasks;
using Autofac;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
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
using Smartstore.Web.Models.Cart;

namespace Smartstore.Web.Tests.Web;

[TestFixture]
public class WishlistCurrencyMappingTests
{
    [TestCase(0.5, 10, 20)]
    [TestCase(1, 20, 40)]
    [TestCase(1.5, 30, 60)]
    [TestCase(2, 40, 80)]
    public async Task Already_converted_wishlist_prices_are_not_exchanged_twice(
        decimal workingCurrencyRate,
        decimal expectedUnitPrice,
        decimal expectedSubtotal)
    {
        var primaryCurrency = new Currency { Id = 1, CurrencyCode = "USD", Rate = 1 };
        var workingCurrency = workingCurrencyRate == 1
            ? primaryCurrency
            : new Currency { Id = 2, CurrencyCode = "EUR", Rate = workingCurrencyRate };

        var product = new Product { Id = 123 };
        var customer = new Customer { Id = 456 };
        var item = new OrganizedShoppingCartItem(new ShoppingCartItem
        {
            Id = 789,
            Product = product,
            Customer = customer,
            Quantity = 2,
            ShoppingCartType = ShoppingCartType.Wishlist
        });

        var workContext = new Mock<IWorkContext>();
        workContext.SetupGet(x => x.WorkingCurrency).Returns(workingCurrency);

        var currencies = new Mock<ICurrencyService>();
        currencies.SetupGet(x => x.PrimaryCurrency).Returns(primaryCurrency);
        currencies.SetupGet(x => x.PrimaryExchangeCurrency).Returns(primaryCurrency);

        var services = new Mock<ICommonServices>();
        services.SetupGet(x => x.WorkContext).Returns(workContext.Object);
        services.SetupGet(x => x.CurrencyService).Returns(currencies.Object);

        using var db = new SmartDbContext(new DbContextOptionsBuilder<SmartDbContext>().Options);
        var store = new Store();
        var batch = new ProductBatchContext(null, db, Mock.Of<IComponentContext>(), store, customer, false);
        var options = new PriceCalculationOptions(batch, customer, store, new Language(), workingCurrency);
        var calculationContext = new PriceCalculationContext(product, item.Item.Quantity, options);

        var unitPrice = new CalculatedPrice(product)
        {
            FinalPrice = new Money(expectedUnitPrice, workingCurrency),
            Saving = new PriceSaving
            {
                HasSaving = true,
                SavingAmount = new Money(2m * workingCurrencyRate, workingCurrency),
                SavingPrice = new Money(22m * workingCurrencyRate, workingCurrency),
                SavingPercent = 9.1f
            }
        };
        var subtotal = new CalculatedPrice(product)
        {
            FinalPrice = new Money(expectedSubtotal, workingCurrency),
            DiscountAmount = new Money(4m * workingCurrencyRate, workingCurrency)
        };

        var priceCalculation = new Mock<IPriceCalculationService>();
        priceCalculation.Setup(x => x.CreateDefaultOptions(false, customer, workingCurrency, null))
            .Returns(options);
        priceCalculation.Setup(x => x.CreateCalculationContextAsync(item, options))
            .ReturnsAsync(calculationContext);
        priceCalculation.Setup(x => x.CalculateSubtotalAsync(calculationContext))
            .ReturnsAsync((unitPrice, subtotal));

        var mapper = new TestWishlistMapper(services.Object, priceCalculation.Object)
        {
            PriceSettings = new PriceSettings { ShowOfferBadge = false },
            PriceLabelService = Mock.Of<IPriceLabelService>()
        };
        var model = new WishlistModel.WishlistItemModel { EnteredQuantity = 2 };

        await mapper.MapPriceOnlyAsync(item, model);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model.Price.UnitPrice.Amount, Is.EqualTo(expectedUnitPrice));
            Assert.That(model.Price.SubTotal.Amount, Is.EqualTo(expectedSubtotal));
            Assert.That(model.Price.UnitPrice.Currency, Is.SameAs(workingCurrency));
            Assert.That(model.Price.SubTotal.Currency, Is.SameAs(workingCurrency));
            Assert.That(model.Price.TotalDiscount.Amount, Is.EqualTo(4m * workingCurrencyRate));
            Assert.That(model.Price.Saving.SavingAmount?.Amount, Is.EqualTo(2m * workingCurrencyRate));
            Assert.That(model.Price.Saving.SavingPrice.Amount, Is.EqualTo(22m * workingCurrencyRate));
        }
    }

    private sealed class TestWishlistMapper : WishlistItemModelMapper
    {
        public TestWishlistMapper(ICommonServices services, IPriceCalculationService priceCalculation)
            : base(services, priceCalculation, null, null, new ShoppingCartSettings(), new CatalogSettings(), null)
        {
        }

        public Task MapPriceOnlyAsync(OrganizedShoppingCartItem item, WishlistModel.WishlistItemModel model)
            => MapPriceAsync(item, model);
    }
}
