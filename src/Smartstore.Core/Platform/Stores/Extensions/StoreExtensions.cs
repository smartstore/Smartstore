#nullable enable

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Smartstore.Http;

namespace Smartstore.Core.Stores
{
    public static class StoreExtensions
    {
        /// <inheritdoc cref="GetAbsoluteUrl(Store, PathString, string?)" />
        /// <param name="path">
        /// The path. May start with current request's base path, in which case it is stripped,
        /// because it is assumed that the store host (base URI) already ends with the base path.
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetAbsoluteUrl(this Store store, string? path)
            => GetAbsoluteUrl(store, WebHelper.WebBasePath, path);

        /// <summary>
        /// Generates an absolute URL for the store (scheme + host + pathBase + relativePath). 
        /// </summary>
        /// <param name="store">The store to generate an absolute URL for.</param>
        /// <param name="pathBase">The current request's base application path.</param>
        /// <param name="path">
        /// The path. May start with <paramref name="pathBase"/>, in which case it is stripped,
        /// because it is assumed that the store host (base URI) already ends with the base path.
        /// </param>
        /// <returns>The absolute URL.</returns>
        public static string GetAbsoluteUrl(this Store store, PathString pathBase, string? path)
        {
            Guard.NotNull(store);

            var baseUrl = store.GetBaseUrl();
            
            if (string.IsNullOrEmpty(path))
            {
                return baseUrl;
            }
            
            if (!pathBase.HasValue)
            {
                // If BasePath is empty just concat baseUri and relative path.
                return baseUrl + path.TrimStart('/');
            }

            var pathString = new PathString(path.EnsureStartsWith('/'));

            // Check if relativePath starts, and baseUrl ends with pathBase.
            // If true, strip it before combining.
            if (pathString.StartsWithSegments(pathBase, out var remainingPath) && baseUrl.EndsWith(pathBase + '/'))
            {
                pathString = remainingPath;
            }

            return baseUrl + pathString.Value!.TrimStart('/');
        }

        /// <summary>
        /// <c>true</c> if the store data is valid. Otherwise <c>false</c>.
        /// </summary>
        /// <param name="store">Store entity</param>
        public static bool IsStoreDataValid(this Store store)
        {
            Guard.NotNull(store);

            if (store.Url.IsEmpty())
            {
                return false;
            } 

            if (Uri.TryCreate(store.Url, UriKind.Absolute, out var uri))
            {
                var domain = uri.DnsSafeHost.EmptyNull().ToLower();

                switch (domain)
                {
                    case "www.yourstore.com":
                    case "yourstore.com":
                    case "www.mystore.com":
                    case "mystore.com":
                    case "www.mein-shop.de":
                    case "mein-shop.de":
                        return false;
                    default:
                        return store.Url.IsWebUrl();
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Indicates whether a store contains a specified host
        /// </summary>
        /// <param name="store">Store</param>
        /// <param name="host">Host</param>
        /// <returns>true - contains, false - no</returns>
        public static bool ContainsHostValue(this Store store, string host)
        {
            Guard.NotNull(store);

            if (string.IsNullOrEmpty(host))
            {
                return false;
            }

            var contains = store.ParseHostValues()
                .FirstOrDefault(x => x.Equals(host, StringComparison.InvariantCultureIgnoreCase)) != null;

            return contains;
        }

        /// <summary>
        /// Parse comma-separated hosts
        /// </summary>
        /// <param name="store">Store</param>
        /// <returns>Comma-separated hosts</returns>
        public static string[] ParseHostValues(this Store store)
        {
            Guard.NotNull(store);

            if (string.IsNullOrWhiteSpace(store.Hosts))
            {
                return Array.Empty<string>();
            }

            return store.Hosts
                .Tokenize(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToArray();
        }
    }
}