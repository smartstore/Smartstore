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

        protected abstract string[] Tokens { get; }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual T GetValueFor<T>(string key)
        {
            return TryGetValueFor(key, out T value) ? value : default;
        }

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
