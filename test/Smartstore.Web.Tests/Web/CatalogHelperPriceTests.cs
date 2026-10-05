using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extras.Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Smartstore.Caching;
using Smartstore.Core;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Web.Controllers;
using Smartstore.Web.Models.Catalog;

namespace Smartstore.Web.Tests.Web;

[TestFixture]
public class CatalogHelperPriceTests
{
    [Test]
    public async Task Can_calculate_product_variant_prices()
    {
        using var fixture = new CatalogHelperFixture(maxCalculations: 1);
        var selection1 = new ProductVariantAttributeSelection(null);
        var selection2 = new ProductVariantAttributeSelection(null);
        var candidates = new[]
        {
            CreateCandidate(11, selection1),
            CreateCandidate(12, selection2)
        };

        var prices = await fixture.Helper.CalculateProductVariantPricesAsync(fixture.Context, candidates, 3);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.CalculationContexts, Has.Count.EqualTo(1));
            Assert.That(fixture.CalculationContexts[0].Quantity, Is.EqualTo(3));
            Assert.That(prices, Has.Count.EqualTo(2));
            Assert.That(prices[11], Is.SameAs(fixture.Price));
            Assert.That(prices[12], Is.SameAs(fixture.Price));
        }
    }

    [Test]
    public async Task Can_calculate_product_variant_prices_with_exceeded_limit()
    {
        using var fixture = new CatalogHelperFixture(maxCalculations: 1);
        var selection1 = new ProductVariantAttributeSelection(null);
        var selection2 = new ProductVariantAttributeSelection(null);
        selection1.AddAttribute(1, [11]);
        selection2.AddAttribute(1, [12]);

        var candidates = new[]
        {
            CreateCandidate(11, selection1),
            CreateCandidate(12, selection2)
        };

        var prices = await fixture.Helper.CalculateProductVariantPricesAsync(fixture.Context, candidates, 1);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(prices, Is.Empty);
            Assert.That(fixture.CalculationContexts, Is.Empty);
        }
    }

    [Test]
    public async Task Can_calculate_product_variant_prices_with_combination()
    {
        using var fixture = new CatalogHelperFixture(maxCalculations: 1);
        var selection = new ProductVariantAttributeSelection(null);
        var combination = new ProductVariantAttributeCombination { Id = 7, Price = 19.90M };

        await fixture.Helper.CalculateProductVariantPricesAsync(
            fixture.Context,
            [CreateCandidate(42, selection, combination)],
            5);

        var calculationContext = fixture.CalculationContexts[0];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(calculationContext.Product, Is.SameAs(fixture.Context.Product));
            Assert.That(calculationContext.Quantity, Is.EqualTo(5));
            Assert.That(calculationContext.AttributeCombination, Is.SameAs(combination));
            Assert.That(calculationContext.AssociatedProducts, Is.SameAs(fixture.Context.AssociatedProducts));
            Assert.That(calculationContext.BundleItem, Is.SameAs(fixture.Context.ProductBundleItem));
            Assert.That(calculationContext.Options.Customer, Is.SameAs(fixture.Context.Customer));
            Assert.That(calculationContext.Options.TargetCurrency, Is.SameAs(fixture.Context.Currency));
            Assert.That(calculationContext.Options.TaxFormat, Is.Null);
        }
    }

    private static ProductVariantCandidate CreateCandidate(
        int attributeValueId,
        ProductVariantAttributeSelection selection,
        ProductVariantAttributeCombination combination = null)
    {
        return new()
        {
            AttributeValue = new ProductVariantAttributeValue { Id = attributeValueId },
            Selection = selection,
            Combination = combination
        };
    }

    private sealed class CatalogHelperFixture : IDisposable
    {
        private readonly AutoMock _autoMock;
        private readonly SmartDbContext _db;

        public CatalogHelperFixture(int maxCalculations)
        {
            var product = new Product { Id = 1 };
            var customer = new Customer();
            var store = new Store();
            var language = new Language();
            var currency = new Currency();

            _db = new SmartDbContext(new DbContextOptionsBuilder<SmartDbContext>().Options);
            var batchContext = new ProductBatchContext(
                null,
                _db,
                Mock.Of<IComponentContext>(),
                store,
                customer,
                false);
            var options = new PriceCalculationOptions(batchContext, customer, store, language, currency);

            Price = new CalculatedPrice(product);
            CalculationContexts = [];

            _autoMock = AutoMock.GetLoose(builder =>
            {
                builder.RegisterInstance(_db);
                builder.RegisterInstance(new PerformanceSettings { MaxVariantPriceCalculations = maxCalculations });
            });
            _autoMock.Mock<ICommonServices>().SetupGet(x => x.WorkContext).Returns(Mock.Of<IWorkContext>());
            _autoMock.Mock<ICommonServices>().SetupGet(x => x.Cache).Returns(Mock.Of<ICacheManager>());
            _autoMock.Mock<ILanguageService>().Setup(x => x.IsMultiLanguageEnvironment()).Returns(false);
            _autoMock.Mock<IUrlHelper>()
                .SetupGet(x => x.ActionContext)
                .Returns(new ActionContext { HttpContext = new DefaultHttpContext() });
            _autoMock.Mock<IPriceCalculationService>()
                .Setup(x => x.CreateDefaultOptions(false, customer, null, batchContext))
                .Returns(options);
            _autoMock.Mock<IPriceCalculationService>()
                .Setup(x => x.CalculatePriceAsync(It.IsAny<PriceCalculationContext>()))
                .Callback<PriceCalculationContext>(CalculationContexts.Add)
                .ReturnsAsync(Price);

            Helper = _autoMock.Create<CatalogHelper>();
            Context = new ProductDetailsModelContext
            {
                Product = product,
                BatchContext = batchContext,
                Customer = customer,
                Store = store,
                Currency = currency,
                AssociatedProducts = [new Product { Id = 2 }],
                ProductBundleItem = new ProductBundleItem { Id = 3 }
            };
        }

        public CatalogHelper Helper { get; }
        public ProductDetailsModelContext Context { get; }
        public CalculatedPrice Price { get; }
        public List<PriceCalculationContext> CalculationContexts { get; }

        public void Dispose()
        {
            _autoMock.Dispose();
            _db.Dispose();
        }
    }
}
