using System.Globalization;
using System.Text.RegularExpressions;
using Smartstore.Caching;
using Smartstore.Http;
using Smartstore.IO;

namespace Smartstore.Core.Localization
{
    public class LocalizationFileResolver : ILocalizationFileResolver
    {
        const string LangToken = "{lang}";

        private readonly ICacheManager _cache;
        private readonly IApplicationContext _appContext;

        public LocalizationFileResolver(ICacheManager cache, IApplicationContext appContext)
        {
            _cache = cache;
            _appContext = appContext;
        }

        public LocalizationFileResolveResult Resolve(
            string culture,
            string virtualPath,
            bool cache = true,
            string fallbackCulture = "en")
        {
            Guard.NotEmpty(culture, nameof(culture));
            Guard.NotEmpty(virtualPath, nameof(virtualPath));

            var result = new LocalizationFileResolveResult();

            var lastSlashIndex = virtualPath.LastIndexOf('/');
            if (lastSlashIndex <= 0)
            {
                return result;
            }

            // Extract left directory part
            var left = virtualPath.Substring(0, lastSlashIndex);

            // Extract right file (pattern) part
            var pattern = virtualPath[(lastSlashIndex + 1)..];

            if (!pattern.Contains("{lang}"))
            {
                // File part must contain token.
                throw new ArgumentException("The file pattern must contain the wildcard token for substitution, e.g. 'lang-{lang}.js'.", nameof(virtualPath));
            }

            result.FilePattern = pattern;

            if (left.IsEmpty())
            {
                return result;
            }

            left = WebHelper.ToAppRelativePath(left).EnsureEndsWith('/');

            var fileSystem = _appContext.WebRoot;
            var dir = fileSystem.GetDirectory(left);
            if (!dir.Exists)
            {
                return result;
            }

            var cacheKey = "core:locfile:" + left.ToLower() + pattern + "/" + culture;

            if (cache && _cache.TryGet(cacheKey, out string resolvedCulture))
            {
                return CreateResult(result, resolvedCulture, left, pattern);
            }

            if (!CultureHelper.IsValidCultureCode(culture))
            {
                throw new ArgumentException($"'{culture}' is not a valid culture code.", nameof(culture));
            }

            var ci = CultureInfo.GetCultureInfo(culture);
            var directory = _appContext.WebRoot.GetDirectory(left);

            if (!directory.Exists)
            {
                throw new DirectoryNotFoundException($"Path '{left}' does not exist.");
            }

            // 1: Match passed culture
            resolvedCulture = ResolveMatchingFile(ci, directory, pattern);

            if (resolvedCulture == null && fallbackCulture.HasValue() && culture != fallbackCulture)
            {
                if (!CultureHelper.IsValidCultureCode(fallbackCulture))
                {
                    throw new ArgumentException($"'{culture}' is not a valid culture code.", nameof(fallbackCulture));
                }

                // 2: Match fallback culture
                ci = CultureInfo.GetCultureInfo(fallbackCulture);
                resolvedCulture = ResolveMatchingFile(ci, directory, pattern);
            }

            if (cache)
            {
                _cache.Put(cacheKey, resolvedCulture, new CacheEntryOptions().ExpiresIn(TimeSpan.FromHours(24)));
            }

            if (resolvedCulture.HasValue())
            {
                return CreateResult(result, resolvedCulture, left, pattern);
            }

            return result;
        }

        private static LocalizationFileResolveResult CreateResult(LocalizationFileResolveResult result, string resolvedCulture, string dirPath, string pattern)
        {
            if (resolvedCulture.HasValue())
            {
                result.Success = true;
                result.Culture = resolvedCulture;
                result.VirtualPath = dirPath + pattern.Replace(LangToken, resolvedCulture);

                return result;
            }

            return result;
        }

        private static string ResolveMatchingFile(CultureInfo ci, IDirectory directory, string pattern)
        {
            string result = null;
            var fs = directory.FileSystem;
            var dirPath = directory.SubPath.EnsureEndsWith('/');

            // 1: Exact match
            // -----------------------------------------------------
            var fileName = pattern.Replace(LangToken, ci.Name);
            if (fs.FileExists(dirPath + fileName))
            {
                result = ci.Name;
            }

            // 2: Match neutral culture, e.g. de-DE > de
            // -----------------------------------------------------
            if (result == null && !ci.IsNeutralCulture && ci.Parent != null)
            {
                ci = ci.Parent;
                fileName = pattern.Replace(LangToken, ci.Name);
                if (fs.FileExists(dirPath + fileName))
                {
                    result = ci.Name;
                }
            }

            // 2: Match any region, e.g. de-DE > de-CH
            // -----------------------------------------------------
            if (result == null && ci.IsNeutralCulture)
            {
                // Convert pattern to Regex: "lang-*.js" > "^lang.(.+?).js$"
                var rgPattern = "^" + pattern.Replace(LangToken, @"(.+?)") + "$";
                var rgFileName = new Regex(rgPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

                foreach (var fi in directory.EnumerateFiles(pattern.Replace(LangToken, ci.Name + "-*")))
                {
                    var culture = rgFileName.Match(fi.Name).Groups[1].Value;
                    if (CultureHelper.IsValidCultureCode(culture))
                    {
                        result = culture;
                        break;
                    }
                }
            }

            return result;
        }
    }
}
