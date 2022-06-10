using System.Globalization;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class WeekdayRule : IRule
    {
        public static RuleValueSelectListOption[] GetDefaultValues(Language language)
        {
            CultureInfo cultureInfo = null;

            try
            {
                cultureInfo = CultureInfo.GetCultureInfo(language.LanguageCulture);
            }
            catch { }

            var dtif = cultureInfo?.DateTimeFormat ?? DateTimeFormatInfo.InvariantInfo;

            var options = Enum.GetValues(typeof(DayOfWeek))
                .Cast<DayOfWeek>()
                .Select(x => new RuleValueSelectListOption { Value = ((int)x).ToString(), Text = dtif.GetDayName(x) })
                .ToArray();

            return options;
        }

        public Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var match = expression.HasListMatch((int)DateTime.Now.DayOfWeek);

            return Task.FromResult(match);
        }
    }
}
