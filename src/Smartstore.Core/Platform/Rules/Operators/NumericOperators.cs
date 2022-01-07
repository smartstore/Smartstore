namespace Smartstore.Core.Rules.Operators
{
    internal sealed class LessThanOperator : RuleOperator
    {
        internal LessThanOperator()
            : base("<") { }

        protected override Expression GenerateExpression(Expression left, Expression right, IQueryProvider provider)
        {
            return Expression.LessThan(left, right);
        }
    }

    internal sealed class LessThanOrEqualOperator : RuleOperator
    {
        internal LessThanOrEqualOperator()
            : base("<=") { }

        protected override Expression GenerateExpression(Expression left, Expression right, IQueryProvider provider)
        {
            return Expression.LessThanOrEqual(left, right);
        }
    }

    internal sealed class GreaterThanOperator : RuleOperator
    {
        internal GreaterThanOperator()
            : base(">") { }

        protected override Expression GenerateExpression(Expression left, Expression right, IQueryProvider provider)
        {
            return Expression.GreaterThan(left, right);
        }
    }

    internal sealed class GreaterThanOrEqualOperator : RuleOperator
    {
        internal GreaterThanOrEqualOperator()
            : base(">=") { }

        protected override Expression GenerateExpression(Expression left, Expression right, IQueryProvider provider)
        {
            return Expression.GreaterThanOrEqual(left, right);
        }
    }
}
