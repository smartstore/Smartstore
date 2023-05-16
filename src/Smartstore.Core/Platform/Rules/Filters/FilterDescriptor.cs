namespace Smartstore.Core.Rules.Filters
{
    public class FilterDescriptor : RuleDescriptor
    {
        public FilterDescriptor(LambdaExpression memberExpression, RuleScope scope = RuleScope.Customer)
            : base(scope)
        {
            MemberExpression = Guard.NotNull(memberExpression);
        }

        public LambdaExpression MemberExpression { get; private set; }

        public virtual Expression GetExpression(RuleOperator op, Expression valueExpression, IQueryProvider provider)
        {
            return op.GetExpression(MemberExpression.Body, valueExpression, provider);
        }
    }

    public class FilterDescriptor<T, TValue> : FilterDescriptor where T : class
    {
        public FilterDescriptor(Expression<Func<T, TValue>> expression, RuleScope scope = RuleScope.Customer)
            : base(expression, scope) // TODO
        {
            Guard.NotNull(expression);

            MemberExpression = expression;
        }

        public new Expression<Func<T, TValue>> MemberExpression { get; protected set; }
    }
}