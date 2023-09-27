using Moq;
using NUnit.Framework;
using Smartstore.Core;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Common.Services;
using Smartstore.Test.Common;

namespace Smartstore.Tests.Domain
{
    [TestFixture]
    public class CurrencyTests
    {
        IRoundingHelper _roundingHelper;

        [OneTimeSetUp]
        public void SetUp()
        {
            _roundingHelper = new RoundingHelper(new Mock<IWorkContext>().Object, new CurrencySettings());
        }

        [TestCase(0.05, 9.225, 9.20, CurrencyRoundingRule.AlwaysRoundDown)]
        [TestCase(0.05, 9.225, 9.25, CurrencyRoundingRule.AlwaysRoundUp)]
        [TestCase(0.05, 9.24, 9.20, CurrencyRoundingRule.AlwaysRoundDown)]
        [TestCase(0.05, 9.26, 9.30, CurrencyRoundingRule.AlwaysRoundUp)]
        [TestCase(0.1, 9.47, 9.40, CurrencyRoundingRule.AlwaysRoundDown)]
        [TestCase(0.1, 9.47, 9.50, CurrencyRoundingRule.AlwaysRoundUp)]
        [TestCase(0.5, 9.24, 9.00, CurrencyRoundingRule.AlwaysRoundDown)]
        [TestCase(0.5, 9.24, 9.50, CurrencyRoundingRule.AlwaysRoundUp)]
        [TestCase(1.0, 9.77, 9.00, CurrencyRoundingRule.AlwaysRoundDown)]
        [TestCase(1.0, 9.77, 10.00, CurrencyRoundingRule.AlwaysRoundUp)]
        [TestCase(0.05, 9.225, 9.20, CurrencyRoundingRule.RoundMidpointDown)]
        [TestCase(0.1, 9.45, 9.40, CurrencyRoundingRule.RoundMidpointDown)]
        [TestCase(0.5, 9.25, 9.00, CurrencyRoundingRule.RoundMidpointDown)]
        [TestCase(0.05, 9.225, 9.25, CurrencyRoundingRule.RoundMidpointUp)]
        [TestCase(0.05, 9.43, 9.45, CurrencyRoundingRule.RoundMidpointUp)]
        [TestCase(0.05, 9.46, 9.45, CurrencyRoundingRule.RoundMidpointUp)]
        [TestCase(0.05, 9.48, 9.50, CurrencyRoundingRule.RoundMidpointUp)]
        [TestCase(0.1, 9.47, 9.50, CurrencyRoundingRule.RoundMidpointUp)]
        [TestCase(0.1, 9.44, 9.40, CurrencyRoundingRule.RoundMidpointUp)]
        [TestCase(0.5, 9.24, 9.00, CurrencyRoundingRule.RoundMidpointUp)]
        [TestCase(0.5, 9.25, 9.50, CurrencyRoundingRule.RoundMidpointUp)]
        [TestCase(0.5, 9.76, 10.00, CurrencyRoundingRule.RoundMidpointUp)]
        [TestCase(1.0, 9.49, 9.00, CurrencyRoundingRule.RoundMidpointUp)]
        [TestCase(1.0, 9.50, 10.00, CurrencyRoundingRule.RoundMidpointUp)]
        [TestCase(1.0, 9.77, 10.00, CurrencyRoundingRule.RoundMidpointUp)]
        public void Currency_round_to_nearest(decimal denomination, decimal amount, decimal result, CurrencyRoundingRule rule)
        {
            var currency = new Currency
            {
                Id = 1,
                Name = "Euro",
                CurrencyCode = "EUR",
                Rate = 1,
                DisplayLocale = "de-DE",
                Published = true,
                RoundOrderTotalRule = rule,
                RoundOrderTotalDenominator = denomination
            };

            var roundedAmount = _roundingHelper.ToNearest(amount, out _, currency);
            roundedAmount.ShouldEqual(result);
        }
    }
}
