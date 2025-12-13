using System.Text;
using Microsoft.AspNetCore.Routing;

namespace Smartstore;

public static class RouteExtensions
{
    private const string ModuleTokenName = "module";
    private static readonly CompositeFormat _formatRouteIdent = CompositeFormat.Parse("{0}{1}.{2}");

    extension(RouteData routeData)
    {
        public string GetAreaName()
            => Guard.NotNull(routeData).Values.GetAreaName();

        public string GetControllerName()
            => Guard.NotNull(routeData).Values.GetControllerName();

        public string GetActionName()
            => Guard.NotNull(routeData).Values.GetActionName();

        public string GetCultureCode()
            => Guard.NotNull(routeData).Values.GetCultureCode();

        public string GetModuleName()
            => (string)Guard.NotNull(routeData).DataTokens[ModuleTokenName];
    }

    extension(RouteValueDictionary values)
    {
        public string GetAreaName()
        {
            if (values.TryGetValueAs<string>("area", out var value))
            {
                return value;
            }

            return null;
        }

        public string GetControllerName()
        {
            if (values.TryGetValueAs<string>("controller", out var value))
            {
                return value;
            }

            return null;
        }

        public string GetActionName()
        {
            if (values.TryGetValueAs<string>("action", out var value))
            {
                return value;
            }

            return null;
        }

        public string GetCultureCode()
        {
            if (values.TryGetValueAs<string>("culture", out var value))
            {
                return value;
            }

            return null;
        }

        /// <summary>
        /// Generates an identifier for the given route in the form "[{area}.]{controller}.{action}"
        /// </summary>
        public string GenerateRouteIdentifier()
        {
            var area = values.GetAreaName();
            var controller = values.GetControllerName();
            var action = values.GetActionName();

            return _formatRouteIdent.FormatInvariant(area.HasValue() ? area + '.' : string.Empty, controller, action);
        }

        public bool IsSameRoute(string area, string controller, string action)
        {
            if (values == null)
            {
                return false;
            }

            return area.NullEmpty().EqualsNoCase(values.GetAreaName().NullEmpty())
                && controller.EqualsNoCase(values.GetControllerName())
                && action.EqualsNoCase(values.GetActionName());
        }

        public bool IsSameRoute(string controller, string action)
        {
            if (values == null)
            {
                return false;
            }

            return controller.EqualsNoCase(values.GetControllerName())
                && action.EqualsNoCase(values.GetActionName());
        }
    }
}