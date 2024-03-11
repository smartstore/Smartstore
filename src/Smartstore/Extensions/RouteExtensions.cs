using System.Text;
using Microsoft.AspNetCore.Routing;

namespace Smartstore
{
    public static class RouteExtensions
    {
        const string ModuleTokenName = "module";
        
        private readonly static CompositeFormat _formatRouteIdent = CompositeFormat.Parse("{0}{1}.{2}");

        public static string GetAreaName(this RouteData routeData)
            => Guard.NotNull(routeData).Values.GetAreaName();

        public static string GetControllerName(this RouteData routeData)
            => Guard.NotNull(routeData).Values.GetControllerName();

        public static string GetActionName(this RouteData routeData)
            => Guard.NotNull(routeData).Values.GetActionName();

        public static string GetCultureCode(this RouteData routeData)
            => Guard.NotNull(routeData).Values.GetCultureCode();

        public static string GetModuleName(this RouteData routeData)
            => (string)Guard.NotNull(routeData).DataTokens[ModuleTokenName];

        public static string GetAreaName(this RouteValueDictionary values)
        {
            if (values.TryGetValueAs<string>("area", out var value))
            {
                return value;
            }

            return null;
        }

        public static string GetControllerName(this RouteValueDictionary values)
        {
            if (values.TryGetValueAs<string>("controller", out var value))
            {
                return value;
            }

            return null;
        }

        public static string GetActionName(this RouteValueDictionary values)
        {
            if (values.TryGetValueAs<string>("action", out var value))
            {
                return value;
            }

            return null;
        }

        public static string GetCultureCode(this RouteValueDictionary values)
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
        public static string GenerateRouteIdentifier(this RouteValueDictionary values)
        {
            var area = values.GetAreaName();
            var controller = values.GetControllerName();
            var action = values.GetActionName();

            return _formatRouteIdent.FormatInvariant(area.HasValue() ? area + '.' : string.Empty, controller, action);
        }

        public static bool IsSameRoute(this RouteValueDictionary values, string area, string controller, string action)
        {
            if (values == null)
            {
                return false;
            }

            return area.NullEmpty().EqualsNoCase(values.GetAreaName().NullEmpty())
                && controller.EqualsNoCase(values.GetControllerName())
                && action.EqualsNoCase(values.GetActionName());
        }

        public static bool IsSameRoute(this RouteValueDictionary values, string controller, string action)
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