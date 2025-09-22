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
        /// <param name="rightValues">Right values to compare. Taken from <paramref name="expression"/> If <c>null</c> (default).</param>
        /// <param name="comparer">Equality comparer.</param>
        /// <returns><c>true</c> if value matches a list, otherwise <c>false</c>.</returns>
        public static bool HasListMatch<T>(this RuleExpression expression, 
            T value,
            List<T> rightValues = null,
            IEqualityComparer<T> comparer = null)
        {
            rightValues ??= expression.Value as List<T>;
            if (rightValues.IsNullOrEmpty())
            {
                return true;
            }

            if (Equals(value, default(T)))
            {
                return false;
            }
            else if (expression.Operator == RuleOperator.In)
            {
                return rightValues.Contains(value, comparer);
            }
            else if (expression.Operator == RuleOperator.NotIn)
            {
                return !rightValues.Contains(value, comparer);
            }

            throw new InvalidRuleOperatorException(expression);
        }

        /// <summary>
        /// Checks whether a list of values matches a list.
        /// </summary>
        /// <param name="expression">Rule expression.</param>
        /// <param name="values">Values.</param>
        /// <param name="rightValues">Right values to compare. Taken from <paramref name="expression"/> If <c>null</c> (default).</param>
        /// <param name="comparer">Equality comparer.</param>
        /// <returns><c>true</c> values matches a list, otherwise <c>false</c>.</returns>
        public static bool HasListsMatch<T>(this RuleExpression expression,
            IEnumerable<T> values,
            List<T> rightValues = null,
            IEqualityComparer<T> comparer = null)
        {
            rightValues ??= expression.Value as List<T>;
            if (rightValues.IsNullOrEmpty())
            {
                return true;
            }
            //$"- {expression.Operator} {string.Join(",", values)} {string.Join(",", rightValues)}".Dump();

            if (expression.Operator == RuleOperator.IsEqualTo || expression.Operator == RuleOperator.IsNotEqualTo)
            {
                if (values.TryGetNonEnumeratedCount(out var leftCount))
                {
                    leftCount = values.Count();
                }

                var shouldEqual = expression.Operator == RuleOperator.IsEqualTo;
                if (leftCount != rightValues.Count)
                {
                    return !shouldEqual;
                }

                var leftOrdered = values.Order();
                var rightOrdered = rightValues.Order();
                var sequenceEqual = leftOrdered.SequenceEqual(rightOrdered, comparer);

                return shouldEqual ? sequenceEqual : !sequenceEqual;
            }
            else if (expression.Operator == RuleOperator.Contains)
            {
                // Left contains ALL values of right.
                return rightValues.All(x => values.Contains(x, comparer));
            }
            else if (expression.Operator == RuleOperator.NotContains)
            {
                // Left contains NO value of right.
                return rightValues.All(x => !values.Contains(x, comparer));
            }
            else if (expression.Operator == RuleOperator.In)
            {
                // At least one value left is included right.
                return values.Any(x => rightValues.Contains(x, comparer));
            }
            else if (expression.Operator == RuleOperator.NotIn)
            {
                // At least one value left is missing right.
                return values.Any(x => !rightValues.Contains(x, comparer));
            }
            else if (expression.Operator == RuleOperator.AllIn)
            {
                // Right contains ALL values of left.
                return values.All(x => rightValues.Contains(x, comparer));
            }
            else if (expression.Operator == RuleOperator.NotAllIn)
            {
                // Right contains NO value of left.
                return values.All(x => !rightValues.Contains(x, comparer));
            }

            throw new InvalidRuleOperatorException(expression);
        }
    }
}
