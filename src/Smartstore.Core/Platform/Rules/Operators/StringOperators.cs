using Smartstore.Core.Rules.Filters;

namespace Smartstore.Core.Rules.Operators
{
    internal sealed class IsNotEmptyOperator : IsEmptyOperator
    {
        internal IsNotEmptyOperator()
            : base("IsNotEmpty", true) { }
    }

    internal class IsEmptyOperator : RuleOperator
    {
        internal IsEmptyOperator()
            : this("IsEmpty", false) { }

        protected IsEmptyOperator(string op, bool negate)
            : base(op)
        {
            Negate = negate;
        }

        public bool Negate { get; }

        protected override Expression GenerateExpression(Expression left, Expression right, IQueryProvider provider)
        {
            return Expression.Equal(
                left.CallIsNullOrEmpty(),
                Negate ? ExpressionHelper.FalseLiteral : ExpressionHelper.TrueLiteral);
        }
    }

    internal sealed class StartsWithOperator : RuleOperator
    {
        internal StartsWithOperator()
            : base("StartsWith") { }

        protected override Expression GenerateExpression(Expression left, Expression right, IQueryProvider provider)
        {
            var methodInfo = ExpressionHelper.StringStartsWithMethod;
            return Expression.Equal(
                methodInfo.ToCaseInsensitiveStringMethodCall(left, right, provider),
                ExpressionHelper.TrueLiteral);
        }
    }

    internal sealed class EndsWithOperator : RuleOperator
    {
        internal EndsWithOperator()
            : base("EndsWith") { }

        protected override Expression GenerateExpression(Expression left, Expression right, IQueryProvider provider)
        {
            var methodInfo = ExpressionHelper.StringEndsWithMethod;
            return Expression.Equal(
                methodInfo.ToCaseInsensitiveStringMethodCall(left, right, provider),
                ExpressionHelper.TrueLiteral);
        }
    }

    internal sealed class NotContainsOperator : ContainsOperator
    {
        internal NotContainsOperator()
            : base("NotContains", true) { }
    }

    internal class ContainsOperator : RuleOperator
    {
        internal ContainsOperator()
            : this("Contains", false) { }

        protected ContainsOperator(string op, bool negate)
            : base(op)
        {
            Negate = negate;
        }

        public bool Negate { get; }

        protected override Expression GenerateExpression(Expression left, Expression right, IQueryProvider provider)
        {
            return Expression.Equal(
                ExpressionHelper.StringContainsMethod.ToCaseInsensitiveStringMethodCall(left, right, provider),
                Negate ? ExpressionHelper.FalseLiteral : ExpressionHelper.TrueLiteral);
        }
    }
}
