namespace Smartstore.Core.Rules.Filters
{
    public class FilterExpression : RuleExpression
    {
        public new FilterDescriptor Descriptor { get; set; }

        /// <summary>
        /// Optional logical operator to combine with right side expression. Only relevant
        /// if expression is part of a <see cref="FilterExpressionGroup"/>. If <c>null</c>,
        /// the parent group's logical operator is used to combine with right side expression.
        /// </summary>
        public LogicalRuleOperator? LogicalOperator { get; set; }

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
