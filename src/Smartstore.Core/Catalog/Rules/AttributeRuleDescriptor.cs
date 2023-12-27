using Smartstore.Core.Rules;

namespace Smartstore.Core.Catalog.Rules
{
    public class AttributeRuleDescriptor : RuleDescriptor
    {
        public AttributeRuleDescriptor() 
            : base(RuleScope.ProductAttribute)
        {
        }

        public Type ProcessorType { get; init; }
    }
}
