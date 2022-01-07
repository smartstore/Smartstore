using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules
{
    public static partial class RuleExpressionExtensions
    {
        /// <summary>
        /// Checks whether a value matches a list.
        /// </summary>
        /// <param name="expression">Rule expression.</param>
        /// <param name="value">Value.</param>
        /// <param name="comparer">Equality comparer.</param>
        /// <returns><c>true</c> value matches a list, otherwise <c>false</c>.</returns>
        public static bool HasListMatch<T>(this RuleExpression expression, T value, IEqualityComparer<T> comparer = null)
        {
            var right = expression.Value as List<T>;
            if (!(right?.Any() ?? false))
            {
                return true;
            }

            if (object.Equals(value, default(T)))
            {
                return false;
            }
            else if (expression.Operator == RuleOperator.In)
            {
                return right.Contains(value, comparer);
            }
            else if (expression.Operator == RuleOperator.NotIn)
            {
                return !right.Contains(value, comparer);
            }

            throw new InvalidRuleOperatorException(expression);
        }

        /// <summary>
        /// Checks whether a list of values matches a list.
        /// </summary>
        /// <param name="expression">Rule expression.</param>
        /// <param name="values">Values.</param>
        /// <param name="comparer">Equality comparer.</param>
        /// <returns><c>true</c> values matches a list, otherwise <c>false</c>.</returns>
        public static bool HasListsMatch<T>(this RuleExpression expression, IEnumerable<T> values, IEqualityComparer<T> comparer = null)
        {
            var right = expression.Value as List<T>;
            if (!(right?.Any() ?? false))
            {
                return true;
            }

            if (expression.Operator == RuleOperator.IsEqualTo)
            {
                return !right.Except(values, comparer).Any();
            }
            else if (expression.Operator == RuleOperator.IsNotEqualTo)
            {
                return right.Except(values, comparer).Any();
            }
            else if (expression.Operator == RuleOperator.Contains)
            {
                // FALSE for left { 3,2,1 } and right { 0,1,2,3 }.
                return right.All(x => values.Contains(x, comparer));
            }
            else if (expression.Operator == RuleOperator.NotContains)
            {
                return right.All(x => !values.Contains(x, comparer));
            }
            else if (expression.Operator == RuleOperator.In)
            {
                return values.Any(x => right.Contains(x, comparer));
            }
            else if (expression.Operator == RuleOperator.NotIn)
            {
                return values.Any(x => !right.Contains(x, comparer));
            }
            else if (expression.Operator == RuleOperator.AllIn)
            {
                // TRUE for left { 3,2,1 } and right { 0,1,2,3 }.
                return values.All(x => right.Contains(x, comparer));
            }
            else if (expression.Operator == RuleOperator.NotAllIn)
            {
                return values.All(x => !right.Contains(x, comparer));
            }

            throw new InvalidRuleOperatorException(expression);
        }
    }
}
