using Microsoft.AspNetCore.Routing;

namespace Smartstore
{
    public static class RouteExtensions
    {
        public static string GetAreaName(this RouteValueDictionary values)
        {
            if (values.TryGetValue("area", out object value))
            {
                return (value as string);
            }

            return null;
        }

        public static string GetControllerName(this RouteValueDictionary values)
        {
            if (values.TryGetValue("controller", out object value))
            {
                return (value as string);
            }

            return null;
        }

        public static string GetActionName(this RouteValueDictionary values)
        {
            if (values.TryGetValue("action", out object value))
            {
                return (value as string);
            }

            return null;
        }

        public static string GetCultureCode(this RouteValueDictionary values)
        {
            if (values.TryGetValue("culture", out object value))
            {
                return (value as string);
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

            return "{0}{1}.{2}".FormatInvariant(area.HasValue() ? area + '.' : string.Empty, controller, action);
        }

        public static bool IsSameRoute(this RouteValueDictionary values, string area, string controller, string action)
        {
            if (values == null)
                return false;

            return area.EqualsNoCase(values.GetAreaName())
                && controller.EqualsNoCase(values.GetControllerName())
                && action.EqualsNoCase(values.GetActionName());
        }

        public static bool IsSameRoute(this RouteValueDictionary values, string controller, string action)
        {
            if (values == null)
                return false;

            return controller.EqualsNoCase(values.GetControllerName())
                && action.EqualsNoCase(values.GetActionName());
        }
    }
}