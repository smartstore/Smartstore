using System.Globalization;
using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace Smartstore.Core.Localization.Routing
{
    // TODO: (core Remove CultureActionConstraint (?)
    public class CultureActionConstraint : IActionConstraint
    {
        public int Order => 300;

        public bool Accept(ActionConstraintContext context)
        {
            var requestCulture = context.RouteContext.RouteData.Values.GetCultureCode() ?? EnsureNeutral(CultureInfo.CurrentUICulture).Name;

            if (requestCulture != null)
            {
                return CultureHelper.IsValidCultureCode(requestCulture);
            }

            return false;
        }

        public static CultureInfo EnsureNeutral(CultureInfo culture)
        {
            Guard.NotNull(culture, nameof(culture));

            return culture.IsNeutralCulture
                ? culture
                : culture.Parent;
        }
    }
}
