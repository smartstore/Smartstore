using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Smartstore.Utilities;

namespace Smartstore
{
    public static class HttpContextExtensions
    {
        public static ILifetimeScope GetServiceScope(this HttpContext httpContext)
        {
            return httpContext.RequestServices.AsLifetimeScope();
        }

        /// <summary>
        /// Gets a typed route value from <see cref="Microsoft.AspNetCore.Routing.RouteData.Values"/> associated
        /// with the provided <paramref name="httpContext"/>.
        /// </summary>
        /// <typeparam name="T">The type to convert the route value to.</typeparam>
        /// <param name="key">The key of the route value.</param>
        /// <param name="defaultValue">The default value to return if route parameter does not exist.</param>
        /// <returns>The corresponding typed route value, or passed <paramref name="defaultValue"/>.</returns>
        public static T GetRouteValueAs<T>(this HttpContext httpContext, string key, T defaultValue = default)
        {
            if (httpContext.TryGetRouteValueAs(key, out T value))
            {
                return value;
            }

            return defaultValue;
        }

        /// <summary>
        /// Tries to read a route value from <see cref="Microsoft.AspNetCore.Routing.RouteData.Values"/> associated
        /// with the provided <paramref name="httpContext"/>, and converts it to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to convert the route value to.</typeparam>
        /// <param name="key">The key of the route value.</param>
        /// <param name="value">The found and converted value.</param>
        /// <returns><c>true</c> if a value with passed <paramref name="key"/> was present and could be converted, <c>false</c> otherwise.</returns>
        public static bool TryGetRouteValueAs<T>(this HttpContext httpContext, string key, out T value)
        {
            value = default;

            var routeValue = httpContext.GetRouteValue(key);
            if (routeValue != null)
            {
                return ConvertUtility.TryConvert(routeValue, out value);
            }

            return false;
        }

        public static T GetItem<T>(this HttpContext httpContext, string key, Func<T> factory = null, bool forceCreation = true)
        {
            Guard.NotEmpty(key, nameof(key));

            var items = httpContext?.Items;
            if (items == null)
            {
                return default;
            }

            if (items.ContainsKey(key))
            {
                return (T)items[key];
            }
            else
            {
                if (forceCreation)
                {
                    var item = items[key] = (factory ?? (() => default)).Invoke();
                    return (T)item;
                }
                else
                {
                    return default;
                }
            }
        }

        public static async Task<T> GetItemAsync<T>(this HttpContext httpContext, string key, Func<Task<T>> factory, bool forceCreation = true)
        {
            Guard.NotEmpty(key, nameof(key));
            Guard.NotNull(factory, nameof(factory));

            var items = httpContext?.Items;
            if (items == null)
            {
                return default;
            }

            if (items.ContainsKey(key))
            {
                return (T)items[key];
            }
            else
            {
                if (forceCreation)
                {
                    var item = items[key] = await factory();
                    return (T)item;
                }
                else
                {
                    return default;
                }
            }
        }
    }
}