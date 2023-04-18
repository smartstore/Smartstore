using Smartstore.Core.Rules.Filters;

namespace Smartstore.Core.Rules.Operators
{
    internal class NotInOperator : InOperator
    {
        internal NotInOperator()
            : base("NotIn", true) { }
    }

    internal class InOperator : RuleOperator
    {
        internal InOperator()
            : this("In", false) { }

        protected InOperator(string op, bool negate)
            : base(op)
        {
            Negate = negate;
        }

        private bool Negate { get; set; }

        protected override Expression GenerateExpression(Expression left /* member expression */, Expression right /* collection instance */, IQueryProvider provider)
        {
            if (right is not ConstantExpression constantExpr)
            {
                throw new ArgumentException($"The expression must be of type '{nameof(ConstantExpression)}'.", nameof(right));
            }

            var rightType = constantExpr.Type;
            if (constantExpr.Value == null || !(rightType.IsClosedGenericTypeOf(typeof(ICollection<>)) && rightType.IsEnumerableType(out var itemType)))
            {
                throw new ArgumentException("The 'In' operator only supports non-null instances from types that implement 'ICollection<T>'.", nameof(right));
            }

            var containsMethod = ExpressionHelper.GetCollectionContainsMethod(itemType);

            return Expression.Equal(
                Expression.Call(right, containsMethod, left),
                Negate ? ExpressionHelper.FalseLiteral : ExpressionHelper.TrueLiteral);
        }
    }


    internal class NotAllInOperator : AllInOperator
    {
        internal NotAllInOperator()
            : base("NotAllIn", true) { }
    }

    internal class AllInOperator : RuleOperator
    {
        internal AllInOperator()
            : base("AllIn")
        {
        }

        protected AllInOperator(string op, bool negate)
            : base(op)
        {
            Negate = negate;
        }

        private bool Negate { get; set; }

        protected override Expression GenerateExpression(Expression left, Expression right, IQueryProvider provider)
        {
            throw new NotImplementedException();
        }
    }
}
