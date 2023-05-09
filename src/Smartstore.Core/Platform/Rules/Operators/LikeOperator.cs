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
        enum PatternMatchType : byte
        {
            Like,
            Contains,
            StartsWith,
            EndsWith
        }
        
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
            if (right is not ConstantExpression constantExpr)
            {
                throw new ArgumentException($"The expression must be of type '{nameof(ConstantExpression)}'.", nameof(right));
            } 

            if (constantExpr.Value == null || constantExpr.Type != typeof(string))
            {
                throw new ArgumentException("The 'Like' operator only supports non-null strings.", nameof(right));
            }

            var pattern = (string)constantExpr.Value;
            var matchType = PatternMatchType.Like;

            var anyCharIndex = pattern.IndexOf('*');
            var singleCharIndex = pattern.IndexOf('?');

            var hasAnyCharToken = anyCharIndex > -1;
            var hasSingleCharToken = singleCharIndex > -1;
            var hasAnyWildcard = hasAnyCharToken || hasSingleCharToken;

            if (!hasAnyWildcard)
            {
                // Don't attemp GLOB matching if input has no valid GLOB chars
                matchType = PatternMatchType.Contains;
            }
            else if (!hasSingleCharToken)
            {
                // If '?' is present in pattern we MUST perform GLOB match
                if (anyCharIndex == 0 && pattern.LastIndexOf('*') == anyCharIndex)
                {
                    // Pattern starts with '*' and it is the only '*': EndsWith()
                    matchType = PatternMatchType.EndsWith;
                    // Strip leading '*'
                    pattern = pattern[1..];
                }
                else if (pattern.Length > 1 && anyCharIndex == pattern.Length - 1)
                {
                    // Pattern ends with '*' and it is the only '*': StartsWith()
                    matchType = PatternMatchType.StartsWith;
                    // Strip trailing '*'
                    pattern = pattern[..^1];
                }
            }

            Expression expression;

            if (matchType == PatternMatchType.Contains)
            {
                // Call "input".Contains(pattern)
                expression = ExpressionHelper.StringContainsMethod.ToCaseInsensitiveStringMethodCall(left, Expression.Constant(pattern), provider);
            }
            else if (matchType == PatternMatchType.StartsWith)
            {
                // Call "input".StartsWith(pattern)
                expression = ExpressionHelper.StringStartsWithMethod.ToCaseInsensitiveStringMethodCall(left, Expression.Constant(pattern), provider);
            }
            else if (matchType == PatternMatchType.EndsWith)
            {
                // Call "input".EndsWith(pattern)
                expression = ExpressionHelper.StringEndsWithMethod.ToCaseInsensitiveStringMethodCall(left, Expression.Constant(pattern), provider);
            }
            else
            {
                // Handle LIKE
                var isEF = provider is EntityQueryProvider;
                if (isEF)
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

                    var likeMethod = hasUnderscore ? ExpressionHelper.DbLikeMethodWithEscape : ExpressionHelper.DbLikeMethod;
                    // this DbFunctions, matchExpression, pattern, [escapeCharacter]
                    var arguments = hasUnderscore 
                        ? new Expression[] { Expression.Constant(EF.Functions), left, right, Expression.Constant("/") }
                        : new Expression[] { Expression.Constant(EF.Functions), left, right };

                    // Call EF.Functions.Like()
                    expression = Expression.Call(likeMethod, arguments);
                }
                else
                {
                    var wildcard = new Wildcard(pattern);

                    // Call Wildcard.IsMatch()
                    expression = Expression.Call(Expression.Constant(wildcard), ExpressionHelper.WildcardIsMatchMethod, left);
                }
            }

            return Expression.Equal(
                expression,
                Negate ? ExpressionHelper.FalseLiteral : ExpressionHelper.TrueLiteral);
        }
    }
}
