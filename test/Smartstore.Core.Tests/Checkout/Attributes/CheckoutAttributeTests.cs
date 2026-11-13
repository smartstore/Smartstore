using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Smartstore.Caching;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Rules;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Stores;
using Smartstore.Core.Web;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Checkout.Attributes;

[TestFixture]
public class CheckoutAttributeTests : ServiceTestBase
{
    CheckoutAttributeMaterializer _materializer;
    Mock<ICartRuleProvider> _cartRuleProvider;
    ShoppingCart _cart;
    Store _store;
    int _unrestrictedAttributeId;
    int _matchingAttributeId;
    int _nonMatchingAttributeId;
    int _multipleRuleSetsAttributeId;
    int _matchingRuleSetId;
    int _nonMatchingRuleSetId;
    int _matchingValueId;
    int _nonMatchingValueId;
    Action<CartRuleContext> _contextAction;

    [OneTimeSetUp]
    public new async Task SetUp()
    {
        _store = new Store { Id = 1, Name = "Test store" };
        var customer = new Customer { Id = 92837 };
        _cart = new ShoppingCart(customer, _store.Id, []);

        var matchingRuleSet = new RuleSetEntity { Name = "Matching", Scope = RuleScope.Cart };
        var nonMatchingRuleSet = new RuleSetEntity { Name = "Non-matching", Scope = RuleScope.Cart };
        var alternativeRuleSet = new RuleSetEntity { Name = "Alternative", Scope = RuleScope.Cart };

        var unrestrictedAttribute = new CheckoutAttribute { Name = "Unrestricted" };
        var matchingAttribute = new CheckoutAttribute
        {
            Name = "Matching",
            AttributeControlType = AttributeControlType.DropdownList
        };
        var nonMatchingAttribute = new CheckoutAttribute
        {
            Name = "Non-matching",
            AttributeControlType = AttributeControlType.DropdownList
        };
        var multipleRuleSetsAttribute = new CheckoutAttribute { Name = "Multiple rule sets" };
        var matchingValue = new CheckoutAttributeValue { Name = "Matching value", CheckoutAttribute = matchingAttribute };
        var nonMatchingValue = new CheckoutAttributeValue { Name = "Non-matching value", CheckoutAttribute = nonMatchingAttribute };

        matchingAttribute.RuleSets.Add(matchingRuleSet);
        nonMatchingAttribute.RuleSets.Add(nonMatchingRuleSet);
        multipleRuleSetsAttribute.RuleSets.Add(matchingRuleSet);
        multipleRuleSetsAttribute.RuleSets.Add(alternativeRuleSet);

        DbContext.CheckoutAttributes.AddRange(
            unrestrictedAttribute,
            matchingAttribute,
            nonMatchingAttribute,
            multipleRuleSetsAttribute);
        DbContext.CheckoutAttributeValues.AddRange(matchingValue, nonMatchingValue);

        await DbContext.SaveChangesAsync();

        _unrestrictedAttributeId = unrestrictedAttribute.Id;
        _matchingAttributeId = matchingAttribute.Id;
        _nonMatchingAttributeId = nonMatchingAttribute.Id;
        _multipleRuleSetsAttributeId = multipleRuleSetsAttribute.Id;
        _matchingRuleSetId = matchingRuleSet.Id;
        _nonMatchingRuleSetId = nonMatchingRuleSet.Id;
        _matchingValueId = matchingValue.Id;
        _nonMatchingValueId = nonMatchingValue.Id;

        _cartRuleProvider = new Mock<ICartRuleProvider>();
        _cartRuleProvider
            .Setup(x => x.CreateExpressionGroupAsync(It.IsAny<int>()))
            .Returns((int ruleSetId) => Task.FromResult<RuleExpressionGroup>(new() { Id = ruleSetId }));
        _cartRuleProvider
            .Setup(x => x.RuleMatchesAsync(
                It.IsAny<RuleExpression[]>(),
                LogicalRuleOperator.And,
                It.IsAny<Action<CartRuleContext>>()))
            .Returns((RuleExpression[] expressions, LogicalRuleOperator _, Action<CartRuleContext> contextAction) =>
            {
                _contextAction = contextAction;
                return Task.FromResult(expressions[0].Id != _nonMatchingRuleSetId);
            });

        var storeContext = new Mock<IStoreContext>();
        storeContext.Setup(x => x.CurrentStore).Returns(_store);
        storeContext.Setup(x => x.GetCachedStores()).Returns(new StoreEntityCache([_store]));

        _materializer = new CheckoutAttributeMaterializer(
            DbContext,
            new Lazy<ICartRuleProvider>(() => _cartRuleProvider.Object),
            NullRequestCache.Instance,
            storeContext.Object);
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public async Task Can_get_checkout_attributes(bool matchRules, bool includeNonMatchingAttribute)
    {
        _cartRuleProvider.Invocations.Clear();
        _contextAction = null;

        var attributes = await _materializer.GetCheckoutAttributesAsync(_cart, _store.Id, matchRules);
        var ids = attributes.Select(x => x.Id).ToArray();

        Assert.That(ids, Does.Contain(_unrestrictedAttributeId));
        Assert.That(ids, Does.Contain(_matchingAttributeId));
        Assert.That(ids, Does.Contain(_multipleRuleSetsAttributeId));
        Assert.That(ids.Contains(_nonMatchingAttributeId), Is.EqualTo(includeNonMatchingAttribute));

        if (!matchRules)
        {
            _cartRuleProvider.Verify(x => x.CreateExpressionGroupAsync(It.IsAny<int>()), Times.Never);
            Assert.That(_contextAction, Is.Null);
            return;
        }

        _cartRuleProvider.Verify(x => x.CreateExpressionGroupAsync(_matchingRuleSetId), Times.Once);
        _cartRuleProvider.Verify(x => x.CreateExpressionGroupAsync(_nonMatchingRuleSetId), Times.Once);
        Assert.That(_contextAction, Is.Not.Null);

        var context = new CartRuleContext(() => 0)
        {
            Customer = new Customer(),
            Store = new Store()
        };
        _contextAction(context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Customer, Is.SameAs(_cart.Customer));
            Assert.That(context.ShoppingCart, Is.SameAs(_cart));
            Assert.That(context.Store, Is.SameAs(_store));
        }
    }

    [Test]
    public async Task Can_get_checkout_attributes_with_rule()
    {
        var attribute = await DbContext.CheckoutAttributes
            .Include(x => x.RuleSets)
            .FirstAsync(x => x.Id == _nonMatchingAttributeId);
        var ruleSetId = attribute.RuleSets.First(x => x.Name == "Non-matching").Id;

        try
        {
            attribute.RuleSets.Clear();
            await DbContext.SaveChangesAsync();
            DbContext.ChangeTracker.Clear();

            var attributesWithoutMapping = await _materializer.GetCheckoutAttributesAsync(_cart, _store.Id);
            Assert.That(attributesWithoutMapping.Select(x => x.Id), Does.Contain(_nonMatchingAttributeId));

            attribute = await DbContext.CheckoutAttributes
                .Include(x => x.RuleSets)
                .FirstAsync(x => x.Id == _nonMatchingAttributeId);

            var ruleSet = await DbContext.RuleSets.FirstAsync(x => x.Id == ruleSetId);
            attribute.RuleSets.Add(ruleSet);
            await DbContext.SaveChangesAsync();

            DbContext.ChangeTracker.Clear();

            var attributesWithMapping = await _materializer.GetCheckoutAttributesAsync(_cart, _store.Id);
            Assert.That(attributesWithMapping.Select(x => x.Id), Does.Not.Contain(_nonMatchingAttributeId));
        }
        finally
        {
            attribute = await DbContext.CheckoutAttributes
                .Include(x => x.RuleSets)
                .FirstAsync(x => x.Id == _nonMatchingAttributeId);

            if (!attribute.RuleSets.Any(x => x.Id == ruleSetId))
            {
                var ruleSet = await DbContext.RuleSets.FirstAsync(x => x.Id == ruleSetId);
                attribute.RuleSets.Add(ruleSet);
                await DbContext.SaveChangesAsync();
            }
        }
    }

    [Test]
    public async Task Can_cache_checkout_attributes()
    {
        using var requestCache = new RequestCache(null);
        var storeContext = new Mock<IStoreContext>();
        storeContext.Setup(x => x.CurrentStore).Returns(_store);
        storeContext.Setup(x => x.GetCachedStores()).Returns(new StoreEntityCache([_store]));

        var materializer = new CheckoutAttributeMaterializer(
            DbContext,
            new Lazy<ICartRuleProvider>(() => _cartRuleProvider.Object),
            requestCache,
            storeContext.Object);

        await materializer.GetCheckoutAttributesAsync(_cart, _store.Id);
        await materializer.GetCheckoutAttributesAsync(_cart, _store.Id);
        Assert.That(requestCache.Items, Has.Count.EqualTo(1));

        await materializer.GetCheckoutAttributesAsync(_cart, _store.Id, false);
        Assert.That(requestCache.Items, Has.Count.EqualTo(2));

        await materializer.GetCheckoutAttributesAsync(_cart, 0);
        Assert.That(requestCache.Items, Has.Count.EqualTo(3));

        var otherCart = new ShoppingCart(new Customer { Id = _cart.Customer.Id + 1 }, _store.Id, []);
        await materializer.GetCheckoutAttributesAsync(otherCart, _store.Id);
        Assert.That(requestCache.Items, Has.Count.EqualTo(3));

        var shippingCart = new ShoppingCart(_cart.Customer, _store.Id,
        [
            new OrganizedShoppingCartItem(new ShoppingCartItem
            {
                Product = new Product { IsShippingEnabled = true }
            })
        ]);
        await materializer.GetCheckoutAttributesAsync(shippingCart, _store.Id);
        Assert.That(requestCache.Items, Has.Count.EqualTo(4));
    }

    [Test]
    public async Task Can_materialize_checkout_attributes()
    {
        var selection = new CheckoutAttributeSelection(null);
        selection.AddAttributeValue(_matchingAttributeId, _matchingValueId);
        selection.AddAttributeValue(_nonMatchingAttributeId, _nonMatchingValueId);

        var customerAttributes = _cart.Customer.GenericAttributes;
        customerAttributes.CheckoutAttributes = selection;
        await customerAttributes.SaveChangesAsync();

        var materializedSelection = await _materializer.MaterializeCheckoutAttributesAsync(_cart);
        customerAttributes.CheckoutAttributes = new CheckoutAttributeSelection(null);
        await customerAttributes.SaveChangesAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                materializedSelection.Selection.GetAttributeValues(_matchingAttributeId).Select(x => x.ToString()).ToArray(),
                Is.EqualTo([_matchingValueId.ToString()]));
            Assert.That(materializedSelection.Attributes.Select(x => x.Id), Does.Contain(_matchingAttributeId));
            Assert.That(materializedSelection.Attributes.Select(x => x.Id), Does.Not.Contain(_nonMatchingAttributeId));
            Assert.That(materializedSelection.SelectedValues.Select(x => x.Id).ToArray(), Is.EqualTo([_matchingValueId]));
        }
    }

    [Test]
    public async Task Can_format_checkout_attributes()
    {
        var workContext = new Mock<IWorkContext>();
        workContext.Setup(x => x.WorkingLanguage).Returns(new Language { Id = 1, LanguageCulture = "en-US" });

        var formatter = new CheckoutAttributeFormatter(
            Mock.Of<ICurrencyService>(),
            workContext.Object,
            Mock.Of<ITaxService>(),
            Mock.Of<ITaxCalculator>(),
            Mock.Of<IWebHelper>(),
            DbContext);

        var unselectedAttribute = new CheckoutAttribute
        {
            Id = 1,
            Name = "Unselected",
            AttributeControlType = AttributeControlType.TextBox
        };
        var selectedAttribute = new CheckoutAttribute
        {
            Id = 2,
            Name = "Selected",
            AttributeControlType = AttributeControlType.TextBox
        };

        var selection = new CheckoutAttributeSelection(null);
        selection.AddAttributeValue(selectedAttribute.Id, "Value");

        var attributes = new MaterializedCheckoutAttributes(
            new Customer(),
            selection,
            [unselectedAttribute, selectedAttribute],
            []);

        var result = await formatter.FormatAttributesAsync(attributes);

        Assert.That(result, Is.EqualTo("Selected: Value"));
    }

    [Test]
    public void Can_remove_shippable_checkout_attributes()
    {
        var attributes = new List<CheckoutAttribute>
        {
            new()
            {
                Id = 1,
                Name = "Attribute 1",
                ShippableProductRequired = false,
            },
            new()
            {
                Id = 2,
                Name = "Attribute 2",
                ShippableProductRequired = true,
            },
            new()
            {
                Id = 3,
                Name = "Attribute 3",
                ShippableProductRequired = false,
            },
            new()
            {
                Id = 4,
                Name = "Attribute 4",
                ShippableProductRequired = true,
            }
        };

        var filtered = attributes.Where(x => !x.ShippableProductRequired).ToList();
        filtered.Count.ShouldEqual(2);
        filtered[0].Id.ShouldEqual(1);
        filtered[1].Id.ShouldEqual(3);
    }
}