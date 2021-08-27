using System.Linq;
using System.Linq.Expressions;

namespace Smartstore.Core.Rules.Filters
{
    public class FilterExpression : RuleExpression
    {
        public new FilterDescriptor Descriptor { get; set; }

        public virtual Expression ToPredicate(ParameterExpression node, IQueryProvider provider)
        {
            return CreateBodyExpression(node, provider);
        }

        protected virtual Expression CreateBodyExpression(ParameterExpression node, IQueryProvider provider)
        {
            return this.Descriptor.GetExpression(
                this.Operator,
                ExpressionHelper.CreateValueExpression(Descriptor.MemberExpression.Body.Type, this.Value),
                provider);
        }
    }
}
