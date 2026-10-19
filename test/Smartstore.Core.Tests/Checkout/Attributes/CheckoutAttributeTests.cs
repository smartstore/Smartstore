using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Smartstore.Core.Catalog.Attributes;
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
using Smartstore.Domain;
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
        _matchingValueId = matchingValue.Id;
        _nonMatchingValueId = nonMatchingValue.Id;

        _cartRuleProvider = new Mock<ICartRuleProvider>();
        _cartRuleProvider
            .Setup(x => x.RuleMatchesAsync(
                It.IsAny<IRulesContainer>(),
                It.IsAny<LogicalRuleOperator>(),
                It.IsAny<Action<CartRuleContext>>()))
            .Returns((IRulesContainer entity, LogicalRuleOperator _, Action<CartRuleContext> contextAction) =>
            {
                _contextAction = contextAction;
                return Task.FromResult(((BaseEntity)entity).Id != _nonMatchingAttributeId);
            });

        var storeContext = new Mock<IStoreContext>();
        storeContext.Setup(x => x.CurrentStore).Returns(_store);
        storeContext.Setup(x => x.GetCachedStores()).Returns(new StoreEntityCache([_store]));

        _materializer = new CheckoutAttributeMaterializer(
            DbContext,
            new Lazy<ICartRuleProvider>(() => _cartRuleProvider.Object),
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
            _cartRuleProvider.Verify(x => x.RuleMatchesAsync(
                It.IsAny<IRulesContainer>(),
                It.IsAny<LogicalRuleOperator>(),
                It.IsAny<Action<CartRuleContext>>()), Times.Never);
            Assert.That(_contextAction, Is.Null);
            return;
        }

        _cartRuleProvider.Verify(x => x.RuleMatchesAsync(
            It.Is<IRulesContainer>(x => ((BaseEntity)x).Id == _multipleRuleSetsAttributeId),
            LogicalRuleOperator.Or,
            It.IsAny<Action<CartRuleContext>>()), Times.Once);
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