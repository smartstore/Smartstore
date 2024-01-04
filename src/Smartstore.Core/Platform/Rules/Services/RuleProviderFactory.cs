using Autofac;
using Smartstore.Core.Catalog.Rules;
using Smartstore.Core.Checkout.Rules;
using Smartstore.Core.Identity.Rules;

namespace Smartstore.Core.Rules
{
    public class RuleProviderFactory(ILifetimeScope lifetimeScope) : IRuleProviderFactory
    {
        private readonly ILifetimeScope _lifetimeScope = lifetimeScope;

        public virtual IRuleProvider GetProvider(RuleScope scope, object context = null)
        {
            switch (scope)
            {
                case RuleScope.Cart:
                    return _lifetimeScope.Resolve<ICartRuleProvider>();
                case RuleScope.Customer:
                    return _lifetimeScope.Resolve<ITargetGroupService>();
                case RuleScope.Product:
                    return _lifetimeScope.Resolve<IProductRuleProvider>();
                case RuleScope.ProductAttribute:
                    var parameter = TypedParameter.From(Guard.NotNull(context as AttributeRuleProviderContext));
                    return _lifetimeScope.Resolve<IAttributeRuleProvider>(parameter);
                default:
                    throw new ArgumentOutOfRangeException(nameof(scope), $"Cannot get rule provider for scope {scope}. There is no known provider for this scope.");
            }

            throw new NotImplementedException();
        }
    }
}
