using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Seo.Routing
{
    public class UrlSegment
    {
        public UrlSegment(string original)
        {
            Original = original;
        }

        /// <summary>
        /// The original value.
        /// </summary>
        public string Original { get; internal set; }

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
        public UrlPolicy(HttpRequest request)
        {
            Guard.NotNull(request, nameof(request));

            var helper = new LocalizedUrlHelper(request);
            var path = helper.StripCultureCode(out var cultureCode);

            Scheme = new UrlSegment(request.Scheme);
            Host = new UrlSegment(request.Host.Value);
            PathBase = new UrlSegment(request.PathBase.Value?.Trim('/'));
            Culture = new UrlSegment(cultureCode);
            Path = new UrlSegment(path);
            QueryString = new UrlSegment(request.QueryString.Value);
            Method = request.Method;
        }

        public UrlSegment Scheme { get; init; }
        public UrlSegment Host { get; init; }
        public string PathBase { get; init; }
        public UrlSegment Culture { get; init; }
        public UrlSegment Path { get; init; }
        public UrlSegment QueryString { get; init; }

        public string Method { get; init; }
        public string DefaultCultureCode { get; init; }

        public LocalizationSettings LocalizationSettings { get; init; }
        public SeoSettings SeoSettings { get; init; }
        public Language WorkingLanguage { get; set; }
        public Endpoint Endpoint { get; set; }


        /// <summary>
        /// Checks whether the current request's (ambient) culture is the system's default culture
        /// </summary>
        public bool IsDefaultCulture
        {
            get => WorkingLanguage.GetTwoLetterISOLanguageName().EqualsNoCase(DefaultCultureCode);
        }

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
        /// <returns></returns>
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
            var combinedPath = CombineSegments(culture, path);

            if (leftMod)
            {
                return UriHelper.BuildAbsolute(
                    Scheme, 
                    new HostString(Host),
                    new PathString(PathBase),
                    new PathString(combinedPath),
                    new QueryString(QueryString));
            }

            return UriHelper.BuildRelative(
                new PathString(PathBase),
                new PathString(combinedPath),
                new QueryString(QueryString));
        }

        public static string CombineSegments(params string[] segments)
        {
            var combined = segments
                .Where(x => x.HasValue())
                .StrJoin('/')
                .EnsureStartsWith('/');

            return combined;
        }
    }
}