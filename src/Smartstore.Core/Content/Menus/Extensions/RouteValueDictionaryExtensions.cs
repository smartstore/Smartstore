using System;
using Microsoft.AspNetCore.Routing;

namespace Smartstore.Core.Content.Menus
{
    public static class RouteValueDictionaryExtensions
    {
        public static void ApplyTo(this RouteValueDictionary routeValues, INavigatable instance, Action<INavigatable, string, string, RouteValueDictionary> callBack)
        {
            RouteValueDictionary values = new RouteValueDictionary();
            GetActionParams(routeValues, out object actionName, out object controllerName, values);
            callBack(instance, (string)actionName, (string)controllerName, values);
        }

        public static TBuilder ApplyTo<TBuilder>(this RouteValueDictionary routeValues, Func<string, string, RouteValueDictionary, TBuilder> callBack)
        {
            RouteValueDictionary values = new RouteValueDictionary();
            GetActionParams(routeValues, out object actionName, out object controllerName, values);
            return callBack((string)actionName, (string)controllerName, values);
        }

        private static void GetActionParams(RouteValueDictionary routeValues, out object actionName, out object controllerName, RouteValueDictionary values)
        {
            routeValues.TryGetValue("action", out actionName);
            routeValues.TryGetValue("controller", out controllerName);
            routeValues.Remove("action");
            routeValues.Remove("controller");
            values.Merge(routeValues);
        }
    }
}
