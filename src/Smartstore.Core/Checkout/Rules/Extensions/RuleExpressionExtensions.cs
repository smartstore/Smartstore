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
        /// <returns><c>true</c> if value matches a list, otherwise <c>false</c>.</returns>
        public static bool HasListMatch<T>(this RuleExpression expression, T value, IEqualityComparer<T> comparer = null)
        {
            if (expression.Value is not List<T> right || right.Count == 0)
            {
                return true;
            }

            if (Equals(value, default(T)))
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
            if (expression.Value is not List<T> right || right.Count == 0)
            {
                return true;
            }

            if (expression.Operator == RuleOperator.IsEqualTo || expression.Operator == RuleOperator.IsNotEqualTo)
            {
                if (values.TryGetNonEnumeratedCount(out var leftCount))
                {
                    leftCount = values.Count();
                }

                var shouldEqual = expression.Operator == RuleOperator.IsEqualTo;
                if (leftCount != right.Count)
                {
                    return !shouldEqual;
                }

                var leftOrdered = values.Order();
                var rightOrdered = right.Order();
                var sequenceEqual = leftOrdered.SequenceEqual(rightOrdered, comparer);

                return shouldEqual ? sequenceEqual : !sequenceEqual;
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
