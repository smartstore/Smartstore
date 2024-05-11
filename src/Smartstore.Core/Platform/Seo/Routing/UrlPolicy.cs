using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Seo.Routing
{
    public class UrlSegment(string original)
    {
        /// <summary>
        /// The original value.
        /// </summary>
        public string Original { get; internal set; } = original;

        private string Modified { get; set; }

        public void Modify(string value)
            => Modified = value;

        public void Strip()
            => Modified = string.Empty;

        public bool IsModified
            => Modified == string.Empty
                ? Original != null
                : Modified != null && Modified != Original;

        /// <summary>
        /// True if either original or modified string is not empty
        /// </summary>
        public bool HasValue
            => Modified == string.Empty
                ? false
                : Modified != null || Original != null;

        public string Value
            => Modified == string.Empty ? null : (Modified ?? Original);

        public override string ToString()
            => Value;

        public static implicit operator string(UrlSegment segment)
            => segment.Value;
    }

    public sealed class UrlPolicy
    {
        private readonly HttpContext _httpContext;
        private readonly IServiceProvider _services;

        private LocalizationSettings _localizationSettings;
        private SeoSettings _seoSettings;
        private Endpoint _endpoint = null;
        private string _defaultCultureCode = null;

        public UrlPolicy(HttpContext httpContext)
        {
            Guard.NotNull(httpContext);

            _httpContext = httpContext;
            _services = httpContext.RequestServices;

            var request = httpContext.Request;
            var helper = new LocalizedUrlHelper(request);
            var path = helper.StripCultureCode(out var cultureCode);

            OriginalPath = request.Path;
            Scheme = new UrlSegment(request.Scheme);
            Host = new UrlSegment(request.Host.Value);
            PathBase = new UrlSegment(request.PathBase.Value);
            Culture = new UrlSegment(cultureCode);
            Path = new UrlSegment(path.TrimStart('/'));
            QueryString = new UrlSegment(request.QueryString.Value);
            Method = request.Method;
            IsLocalizedUrl = cultureCode != null;
        }

        public PathString OriginalPath { get; }
        public UrlSegment Scheme { get; init; }
        public UrlSegment Host { get; init; }
        public string PathBase { get; init; }
        public UrlSegment Culture { get; init; }
        public UrlSegment Path { get; init; }
        public UrlSegment QueryString { get; init; }

        public string Method { get; init; }
        public bool IsLocalizedUrl { get; }

        public string DefaultCultureCode
        {
            get => _defaultCultureCode ??= _services.GetRequiredService<ILanguageService>().GetMasterLanguageSeoCode();
            set => _defaultCultureCode = value;
        }

        public LocalizationSettings LocalizationSettings 
        {
            get => _localizationSettings ??= _services.GetRequiredService<LocalizationSettings>();
            set => _localizationSettings = value;
        }

        public SeoSettings SeoSettings
        {
            get => _seoSettings ??= _services.GetRequiredService<SeoSettings>();
            set => _seoSettings = value;
        }

        public Endpoint Endpoint
        {
            get => _endpoint ??= _httpContext.GetEndpoint();
            set => _endpoint = value;
        }

        /// <summary>
        /// Checks whether the current request's (ambient) culture is the system's default culture
        /// </summary>
        public bool IsDefaultCulture
        {
            get => !Culture.HasValue || Culture.Value.EqualsNoCase(DefaultCultureCode);
        }

        /// <summary>
        /// Checks whether the current endpoint is the result of a dynamic slug route transformation.
        /// </summary>
        /// <remarks>See <see cref="SlugRouteTransformer"/></remarks>
        public bool IsSlugRoute { get; set; }

        /// <summary>
        /// Set this to <c>true</c> to mark the current request URL as invalid
        /// and to return a HTTP 404 response.
        /// </summary>
        public bool IsInvalidUrl { get; set; }

        /// <summary>
        /// Checks whether left part of the request URL (scheme or host) has been modified.
        /// </summary>
        public bool LeftPartIsModified
        {
            get => Scheme.IsModified || Host.IsModified;
        }

        /// <summary>
        /// Checks whether right part of the request URL (culture, path or querystring) has been modified.
        /// </summary>
        public bool RightPartIsModified
        {
            get => Culture.IsModified || Path.IsModified || QueryString.IsModified;
        }

        /// <summary>
        /// Checks whether any part of the request URL has been modified.
        /// </summary>
        public bool IsModified
        {
            get => LeftPartIsModified || RightPartIsModified;
        }

        /// <summary>
        /// Returns the modified URL if any of the segments has been modified, so
        /// that a HTTP redirect can be performed to the new location.
        /// Returns <c>null</c> if no segment has changed. Returns only the relative path
        /// if only right segments has changed, or the full absolute URL (including scheme and host)
        /// if any left segment has changed.
        /// </summary>
        public string GetModifiedUrl()
        {
            var leftMod = LeftPartIsModified;
            var rightMod = RightPartIsModified;

            if (!leftMod && !rightMod)
            {
                return null;
            }

            var culture = Culture.ToString();
            var path = Path.ToString();
            var combinedPath = RouteHelper.NormalizePathComponent(CombineSegments(culture, path));
            var queryString = new QueryString(RouteHelper.NormalizeQueryComponent(QueryString));

            if (leftMod)
            {
                return UriHelper.BuildAbsolute(
                    Scheme,
                    new HostString(Host),
                    PathBase,
                    combinedPath,
                    queryString);
            }

            return UriHelper.BuildRelative(
                PathBase,
                combinedPath,
                queryString);
        }

        public static string CombineSegments(params string[] segments)
        {
            if (segments == null || segments.Length == 0)
            {
                return "/";
            }

            var firstSegment = segments[0].EmptyNull().EnsureStartsWith("/");

            if (segments.Length == 1)
            {
                return firstSegment;
            }

            var combined = new PathString(firstSegment);

            for (var i = 1; i < segments.Length; i++)
            {
                var segment = segments[i];
                if (segment.HasValue())
                {
                    combined = combined.Add(segment.EnsureStartsWith('/'));
                }
            }

            return combined.Value;
        }
    }
}