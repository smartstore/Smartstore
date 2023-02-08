using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Smartstore.Collections;
using Smartstore.Utilities;

namespace Smartstore.Core.Search
{
    public abstract partial class SearchQueryFactoryBase
    {
        protected readonly IHttpContextAccessor _httpContextAccessor;

        private Multimap<string, string> _aliases;

        protected SearchQueryFactoryBase(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Specifies supported search query tokens, e.g. "q" for the search term.
        /// </summary>
        protected abstract string[] Tokens { get; }

        /// <summary>
        /// Gets a map of all search aliases from the query string and body of the request.
        /// An item that is not included in <see cref="Tokens"/> is assumed to be an alias.
        /// </summary>
        protected virtual Multimap<string, string> Aliases
        {
            get
            {
                if (_aliases == null)
                {
                    _aliases = new Multimap<string, string>();

                    var request = _httpContextAccessor?.HttpContext?.Request;
                    if (request != null)
                    {
                        var tokens = Tokens;

                        if (request.HasFormContentType)
                        {
                            request.Form?.Keys
                                .Where(x => x.HasValue() && !tokens.Contains(x))
                                .Select(x => new { key = x, val = request.Form[x] })
                                .Each(x => _aliases.AddRange(x.key, x.val.SelectMany(y => y.SplitSafe(','))));
                        }

                        request.Query?.Keys
                            .Where(x => x.HasValue() && !tokens.Contains(x))
                            .Select(x => new { key = x, val = request.Query[x] })
                            .Each(x => _aliases.AddRange(x.key, x.val.SelectMany(y => y.SplitSafe(','))));
                    }
                }

                return _aliases;
            }
        }

        /// <summary>
        /// Tries to read a request value first from <see cref="HttpRequest.Form"/> (if method is POST), then from
        /// <see cref="HttpRequest.Query"/>, and converts value to <typeparamref name="T"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual T GetValueFor<T>(string key)
        {
            return TryGetValueFor(key, out T value) ? value : default;
        }

        /// <summary>
        /// Tries to read a request value first from <see cref="HttpRequest.Form"/> (if method is POST), then from
        /// <see cref="HttpRequest.Query"/>, and converts value to <typeparamref name="T"/>.
        /// </summary>
        protected virtual bool TryGetValueFor<T>(string key, out T value)
        {
            var request = _httpContextAccessor?.HttpContext?.Request;

            if (request != null && key.HasValue())
            {
                return request.TryGetValueAs(key, out value);
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Tries to parse range filter values from a raw query value.
        /// </summary>
        /// <param name="query">Raw query value to be parsed.</param>
        /// <param name="min">Parsed minimum value, if any.</param>
        /// <param name="max">Parsed maximum value, if any.</param>
        /// <returns><c>true</c> if either <paramref name="min"/> or <paramref name="max"/> is not <c>null</c>. <c>False</c> otherwise.</returns>
        protected virtual bool TryParseRange<T>(string query, out T? min, out T? max) where T : struct
        {
            min = max = null;

            if (query.IsEmpty())
            {
                return false;
            }

            // Format: from~to || from[~] || ~to
            var arr = query.Split('~').Select(x => x.Trim()).Take(2).ToArray();

            ConvertUtility.TryConvert(arr[0], out min);
            if (arr.Length == 2)
            {
                ConvertUtility.TryConvert(arr[1], out max);
            }

            return min != null || max != null;
        }
    }
}
