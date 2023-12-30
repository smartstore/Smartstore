using System.Collections.Concurrent;
using System.Text;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Http;

namespace Smartstore.Core.Widgets
{
    public partial class PageAssetBuilder : IPageAssetBuilder
    {
        private readonly IApplicationContext _appContext;
        private readonly Lazy<IUrlHelper> _urlHelper;
        private readonly IAssetTagGenerator _assetTagGenerator;
        private readonly SeoSettings _seoSettings;

        private List<string> _titleParts;
        private List<string> _metaDescriptionParts;
        private List<string> _metaKeywordParts;

        private static readonly ConcurrentDictionary<string, string> _minFiles = new(StringComparer.InvariantCultureIgnoreCase);

        public PageAssetBuilder(
            IApplicationContext appContext,
            Lazy<IUrlHelper> urlHelper,
            IWidgetProvider widgetProvider,
            IAssetTagGenerator assetTagGenerator,
            SeoSettings seoSettings,
            IStoreContext storeContext)
        {
            _appContext = appContext;
            _urlHelper = urlHelper;
            _seoSettings = seoSettings;
            _assetTagGenerator = assetTagGenerator;
            WidgetProvider = widgetProvider;

            var htmlBodyId = storeContext.CurrentStore.HtmlBodyId;
            if (htmlBodyId.HasValue())
            {
                BodyAttributes["id"] = htmlBodyId;
            }
        }

        public IWidgetProvider WidgetProvider { get; }

        public AttributeDictionary RootAttributes { get; } = new();

        public AttributeDictionary BodyAttributes { get; } = new();

        public virtual void AddTitleParts(IEnumerable<string> parts, bool prepend = false)
            => AddPartsInternal(ref _titleParts, parts, prepend);

        public virtual void AddMetaDescriptionParts(IEnumerable<string> parts, bool prepend = false)
            => AddPartsInternal(ref _metaDescriptionParts, parts, prepend);

        public virtual void AddMetaKeywordParts(IEnumerable<string> parts, bool prepend = false)
            => AddPartsInternal(ref _metaKeywordParts, parts, prepend);

        public virtual void AddScriptFiles(IEnumerable<string> urls, AssetLocation location, bool prepend = false)
        {
            if (urls == null || !urls.Any())
            {
                return;
            }

            string zoneName = location == AssetLocation.Head ? "head_scripts" : "scripts";

            foreach (var src in urls.Select(x => x.Trim()))
            {
                var content =
                    _assetTagGenerator.GenerateScript(src) ??
                    new HtmlString($"<script src=\"{ResolveAssetUrl(src)}\"></script>");

                AddHtmlContent(zoneName, content, src, prepend);
            }
        }

        public virtual void AddCssFiles(IEnumerable<string> urls, bool prepend = false)
        {
            const string zoneName = "stylesheets";

            if (urls == null || !urls.Any())
            {
                return;
            }

            foreach (var href in urls.Select(x => x.Trim()))
            {
                var content =
                    _assetTagGenerator.GenerateStylesheet(href) ??
                    new HtmlString($"<link href=\"{ResolveAssetUrl(href)}\" rel=\"stylesheet\" type=\"text/css\" />");

                AddHtmlContent(zoneName, content, href, prepend);
            }
        }

        private string ResolveAssetUrl(string src)
        {
            if (!_appContext.HostEnvironment.IsDevelopment())
            {
                src = TryFindMinFile(src);
            }

            return _urlHelper.Value.Content(src);
        }

        public virtual void AddHtmlContent(string targetZone, IHtmlContent content, string key = null, bool prepend = false)
        {
            Guard.NotEmpty(targetZone, nameof(targetZone));
            Guard.NotNull(content, nameof(content));

            if (key.HasValue() && WidgetProvider.ContainsWidget(targetZone, key))
            {
                return;
            }

            WidgetProvider.RegisterWidget(
                targetZone,
                new HtmlWidget(content) { Key = key, Prepend = prepend });
        }

        public virtual IHtmlContent GetDocumentTitle(bool addDefaultTitle)
        {
            if (_titleParts == null)
                return HtmlString.Empty;

            var result = string.Empty;
            var currentTitle = string.Join(_seoSettings.PageTitleSeparator, _titleParts.Distinct(StringComparer.CurrentCultureIgnoreCase).Reverse().ToArray());

            if (currentTitle.HasValue())
            {
                if (addDefaultTitle)
                {
                    // Store name + page title
                    switch (_seoSettings.PageTitleSeoAdjustment)
                    {
                        case PageTitleSeoAdjustment.PagenameAfterStorename:
                            result = string.Join(_seoSettings.PageTitleSeparator, _seoSettings.GetLocalizedSetting(x => x.MetaTitle).Value, currentTitle);
                            break;
                        case PageTitleSeoAdjustment.StorenameAfterPagename:
                        default:
                            result = string.Join(_seoSettings.PageTitleSeparator, currentTitle, _seoSettings.GetLocalizedSetting(x => x.MetaTitle).Value);
                            break;
                    }
                }
                else
                {
                    // Page title only
                    result = currentTitle;
                }
            }
            else
            {
                // Store name only
                result = _seoSettings.GetLocalizedSetting(x => x.MetaTitle).Value;
            }

            return new HtmlString(result);
        }

        public virtual IHtmlContent GetMetaDescription()
        {
            string result = null;

            if (_metaDescriptionParts != null)
            {
                result = string.Join(", ", _metaDescriptionParts.Distinct(StringComparer.CurrentCultureIgnoreCase).Reverse().ToArray());
            }

            return new HtmlString(result?.AttributeEncode()?.NullEmpty() ?? _seoSettings.GetLocalizedSetting(x => x.MetaDescription).Value);
        }

        public virtual IHtmlContent GetMetaKeywords()
        {
            string result = null;

            if (_metaKeywordParts != null)
            {
                result = string.Join(", ", _metaKeywordParts.Distinct(StringComparer.CurrentCultureIgnoreCase).Reverse().ToArray());
            }

            return new HtmlString(result?.AttributeEncode()?.NullEmpty() ?? _seoSettings.GetLocalizedSetting(x => x.MetaKeywords).Value);
        }

        public virtual string TryFindMinFile(string path, IFileProvider fileProvider = null)
        {
            Guard.NotEmpty(path, nameof(path));

            path = _minFiles.GetOrAdd(path, key =>
            {
                try
                {
                    if (!WebHelper.IsLocalUrl(key))
                    {
                        // No need to look for external files
                        return key;
                    }

                    var extension = Path.GetExtension(key);
                    if (key.EndsWith(".min" + extension, StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Is already a MIN file, get out!
                        return key;
                    }

                    var minPath = CompositeFormatCache.Get("{0}.min{1}").FormatInvariant(key[..^extension.Length], extension);
                    fileProvider ??= _appContext.WebRoot;
                    if (fileProvider.GetFileInfo(minPath.TrimStart('~', '/')).Exists)
                    {
                        return minPath;
                    }

                    return key;
                }
                catch
                {
                    return key;
                }
            });

            return path;
        }

        #region Utils

        // Helper func: changes all following public funcs to remove code redundancy
        private static void AddPartsInternal<T>(ref List<T> list, IEnumerable<T> partsToAdd, bool prepend = false)
        {
            var parts = (partsToAdd ?? Enumerable.Empty<T>()).Where(IsValidPart);

            if (list == null)
            {
                list = new List<T>(parts);
            }
            else if (parts.Any())
            {
                if (prepend)
                {
                    // Insertion of multiple parts at the beginning
                    // should keep order (and not vice-versa as it was originally)
                    list.InsertRange(0, parts);
                }
                else
                {
                    list.AddRange(parts);
                }
            }
        }

        private static bool IsValidPart<T>(T part)
        {
            return part != null || (part is string str && str.HasValue());
        }

        #endregion
    }
}
