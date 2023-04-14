namespace Smartstore.Core.Rules.Filters
{
    public class FilterDescriptor : RuleDescriptor
    {
        public FilterDescriptor(LambdaExpression memberExpression)
            : base(RuleScope.Customer)
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
        public FilterDescriptor(Expression<Func<T, TValue>> expression)
            : base(expression) // TODO
        {
            Guard.NotNull(expression);

            MemberExpression = expression;
        }

        public new Expression<Func<T, TValue>> MemberExpression { get; protected set; }
    }
}