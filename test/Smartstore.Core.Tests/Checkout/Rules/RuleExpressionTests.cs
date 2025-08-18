using System.Linq;
using NUnit.Framework;
using Smartstore.Core.Checkout.Rules;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Tests.Rules
{
    [TestFixture]
    public class RuleExpressionTests
    {
        private readonly int[] _left = [3, 2, 1];

        [TestCase(new int[] { 1, 2, 3 }, "=")]
        [TestCase(new int[] { 1, 2, 3, 4 }, "=", false)]
        [TestCase(new int[] { 3, 2 }, "!=")]
        [TestCase(new int[] { 1, 2, 3 }, "!=", false)]
        [TestCase(new int[] { 1, 2 }, "Contains")]
        [TestCase(new int[] { 0, 1, 2, 3 }, "Contains", false)]
        [TestCase(new int[] { 4, 5, 6 }, "NotContains")]
        [TestCase(new int[] { 1, 2, 3 }, "NotContains", false)]
        [TestCase(new int[] { 3, 4 }, "In")]
        [TestCase(new int[] { 0, 4 }, "In", false)]
        [TestCase(new int[] { 3, 2 }, "NotIn")]
        [TestCase(new int[] { 1, 2, 3, 4 }, "NotIn", false)]
        [TestCase(new int[] { 1, 2, 3, 4 }, "AllIn")]
        [TestCase(new int[] { 1, 2 }, "AllIn", false)]
        [TestCase(new int[] { 0, 4 }, "NotAllIn")]
        [TestCase(new int[] { 0, 1, 4 }, "NotAllIn", false)]
        public void Rule_has_lists_match(int[] right, string op, bool expected = true)
        {
            var expression = new RuleExpression
            {
                Operator = op,
                Value = right.ToList()
            };

            var result = expression.HasListsMatch(_left);
            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
