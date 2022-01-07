using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules
{
    public class CartRuleDescriptor : RuleDescriptor
    {
        public CartRuleDescriptor() : base(RuleScope.Cart)
        {
        }

        public Type ProcessorType { get; init; }
        //public IRule ProcessorInstance { get; set; }
    }
}
