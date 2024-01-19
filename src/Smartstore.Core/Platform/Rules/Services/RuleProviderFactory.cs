using Autofac;
using Smartstore.Core.Catalog.Rules;
using Smartstore.Core.Checkout.Rules;
using Smartstore.Core.Identity.Rules;

namespace Smartstore.Core.Rules
{
    public class RuleProviderFactory : IRuleProviderFactory
    {
        private readonly ILifetimeScope _scope;

        public RuleProviderFactory(ILifetimeScope lifetimeScope)
        {
            _scope = lifetimeScope;
        }

        public virtual IRuleProvider GetProvider(RuleScope scope, object context = null)
        {
            switch (scope)
            {
                case RuleScope.Cart:
                    return _scope.Resolve<ICartRuleProvider>();
                case RuleScope.Customer:
                    return _scope.Resolve<ITargetGroupService>();
                case RuleScope.Product:
                    return _scope.Resolve<IProductRuleProvider>();
                case RuleScope.ProductAttribute:
                    var parameter = TypedParameter.From(Guard.NotNull(context as AttributeRuleProviderContext));
                    return _scope.Resolve<IAttributeRuleProvider>(parameter);
                default:
                    throw new ArgumentOutOfRangeException(nameof(scope), $"Cannot get rule provider for scope {scope}. There is no known provider for this scope.");
            }
        }
    }
}
