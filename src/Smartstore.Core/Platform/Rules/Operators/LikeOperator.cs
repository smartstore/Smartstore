using Microsoft.EntityFrameworkCore.Query.Internal;
using Smartstore.Core.Rules.Filters;
using Smartstore.Utilities;

namespace Smartstore.Core.Rules.Operators
{
    internal sealed class NotLikeOperator : LikeOperator
    {
        internal NotLikeOperator()
            : base("NotLike", true) { }
    }

    internal class LikeOperator : RuleOperator
    {
        internal LikeOperator()
            : this("Like", false) { }

        protected LikeOperator(string op, bool negate)
            : base(op)
        {
            Negate = negate;
        }

        private bool Negate { get; set; }

        protected override Expression GenerateExpression(Expression left /* member expression */, Expression right /* term */, IQueryProvider provider)
        {
            var constantExpr = right as ConstantExpression;
            if (constantExpr == null)
                throw new ArgumentException($"The expression must be of type '{nameof(ConstantExpression)}'.", nameof(right));

            if (constantExpr.Value == null || constantExpr.Type != typeof(string))
            {
                throw new ArgumentException("The 'Like' operator only supports non-null strings.", nameof(right));
            }

            var isEF = provider is EntityQueryProvider;
            var pattern = (string)constantExpr.Value;

            Expression expression;

            if (isEF)
            {
                var hasAnyCharToken = pattern.IndexOf('*') > -1;
                var hasSingleCharToken = pattern.IndexOf('?') > -1;
                var hasAnyWildcard = hasAnyCharToken || hasSingleCharToken;

                if (hasAnyWildcard)
                {
                    // Convert file wildcard pattern to SQL LIKE expression:
                    // my*new_file-?.png > my%new/_file-_.png

                    var hasUnderscore = pattern.IndexOf('_') > -1;

                    if (hasUnderscore)
                    {
                        pattern = pattern.Replace("_", "/_");
                    }
                    if (hasAnyCharToken)
                    {
                        pattern = pattern.Replace('*', '%');
                    }
                    if (hasSingleCharToken)
                    {
                        pattern = pattern.Replace('?', '_');
                    }

                    right = Expression.Constant(pattern);
                }

                // Call EF.Functions.Like()
                expression = Expression.Call(ExpressionHelper.DbLikeMethod, new Expression[]
                {
                    // this DbFunctions
                    Expression.Constant(EF.Functions),
                    // matchExpression
                    left,
                    // pattern
                    right,
                    // escapeCharacter
                    Expression.Constant("/")
                });
            }
            else
            {
                var wildcard = new Wildcard(pattern);

                // Call Wildcard.IsMatch()
                expression = Expression.Call(Expression.Constant(wildcard), ExpressionHelper.WildcardIsMatchMethod, left);
            }

            return Expression.Equal(
                expression,
                Negate ? ExpressionHelper.FalseLiteral : ExpressionHelper.TrueLiteral);
        }
    }
}
