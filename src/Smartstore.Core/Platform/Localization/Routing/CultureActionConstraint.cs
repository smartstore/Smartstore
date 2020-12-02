using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Routing;

namespace Smartstore.Core.Localization.Routing
{
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
