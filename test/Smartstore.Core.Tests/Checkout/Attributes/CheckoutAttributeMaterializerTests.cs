using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Rules;
using Smartstore.Core.Identity;
using Smartstore.Core.Rules;
using Smartstore.Core.Stores;
using Smartstore.Domain;

namespace Smartstore.Core.Tests.Checkout.Attributes;

[TestFixture]
public class CheckoutAttributeMaterializerTests : ServiceTestBase
{
    CheckoutAttributeMaterializer _materializer;
    Mock<ICartRuleProvider> _cartRuleProvider;
    ShoppingCart _cart;
    Store _store;
    int _unrestrictedAttributeId;
    int _matchingAttributeId;
    int _nonMatchingAttributeId;
    int _multipleRuleSetsAttributeId;
    Action<CartRuleContext> _contextAction;

    [OneTimeSetUp]
    public new async Task SetUp()
    {
        _store = new Store { Id = 1, Name = "Test store" };
        var customer = new Customer { Id = 1 };
        _cart = new ShoppingCart(customer, _store.Id, []);

        var matchingRuleSet = new RuleSetEntity { Name = "Matching", Scope = RuleScope.Cart };
        var nonMatchingRuleSet = new RuleSetEntity { Name = "Non-matching", Scope = RuleScope.Cart };
        var alternativeRuleSet = new RuleSetEntity { Name = "Alternative", Scope = RuleScope.Cart };

        var unrestrictedAttribute = new CheckoutAttribute { Name = "Unrestricted" };
        var matchingAttribute = new CheckoutAttribute { Name = "Matching" };
        var nonMatchingAttribute = new CheckoutAttribute { Name = "Non-matching" };
        var multipleRuleSetsAttribute = new CheckoutAttribute { Name = "Multiple rule sets" };

        matchingAttribute.RuleSets.Add(matchingRuleSet);
        nonMatchingAttribute.RuleSets.Add(nonMatchingRuleSet);
        multipleRuleSetsAttribute.RuleSets.Add(matchingRuleSet);
        multipleRuleSetsAttribute.RuleSets.Add(alternativeRuleSet);

        DbContext.CheckoutAttributes.AddRange(
            unrestrictedAttribute,
            matchingAttribute,
            nonMatchingAttribute,
            multipleRuleSetsAttribute);

        await DbContext.SaveChangesAsync();

        _unrestrictedAttributeId = unrestrictedAttribute.Id;
        _matchingAttributeId = matchingAttribute.Id;
        _nonMatchingAttributeId = nonMatchingAttribute.Id;
        _multipleRuleSetsAttributeId = multipleRuleSetsAttribute.Id;

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
    public async Task Can_load_checkout_attributes(bool matchRules, bool includeNonMatchingAttribute)
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
}