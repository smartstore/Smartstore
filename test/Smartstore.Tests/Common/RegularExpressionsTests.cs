using NUnit.Framework;
using Smartstore.Test.Common;

namespace Smartstore.Tests.Common
{
    [TestFixture]
    public class RegularExpressionsTests
    {
        // 8, 12, 13 or 14 digits.
        [TestCase("29033706", true)]
        [TestCase("0088381848503", true)]
        [TestCase("0123456789012345", false)]
        [TestCase("123456789", false)]
        [TestCase("01234x6789123", false)]
        public void Can_validate_GTIN(string ean, bool result)
        {
            var isMatch = RegularExpressions.IsGtin.IsMatch(ean);
            isMatch.ShouldEqual(result);
        }
    }
}
