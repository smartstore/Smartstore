using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Hosting;
using Smartstore.Core.Content.Seo;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Engine;
using Smartstore.Net;

namespace Smartstore.Core.Widgets
{
    public partial class PageAssetBuilder : IPageAssetBuilder
    {
        private readonly IApplicationContext _appContext;
        private readonly Lazy<IUrlHelper> _urlHelper;
        private readonly SeoSettings _seoSettings;

        private List<string> _titleParts;
        private List<string> _metaDescriptionParts;
        private List<string> _metaKeywordParts;

        private static readonly ConcurrentDictionary<string, string> _minFiles = new(StringComparer.InvariantCultureIgnoreCase);

        public PageAssetBuilder(
            IApplicationContext appContext,
            Lazy<IUrlHelper> urlHelper,
            IWidgetProvider widgetProvider,
            SeoSettings seoSettings,
            IStoreContext storeContext)
        {
            // TODO: (core) IApplicationContext.WebRoot > StaticFileOptions.FileProvider (?)
            _appContext = appContext;
            _urlHelper = urlHelper;
            _seoSettings = seoSettings;
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

        public void AddTitleParts(IEnumerable<string> parts, bool prepend = false)
            => AddPartsInternal(ref _titleParts, parts, prepend);

        public void AddMetaDescriptionParts(IEnumerable<string> parts, bool prepend = false)
            => AddPartsInternal(ref _metaDescriptionParts, parts, prepend);

        public void AddMetaKeywordParts(IEnumerable<string> parts, bool prepend = false)
            => AddPartsInternal(ref _metaKeywordParts, parts, prepend);

        public void AddCanonicalUrlParts(IEnumerable<string> parts, bool prepend = false)
        {
            const string zoneName = "head_canonical";
            
            if (parts == null || !parts.Any())
            {
                return;
            }

            foreach (var href in parts.Where(IsValidPart).Select(x => x.Trim()))
            {
                var partKey = "canonical:" + href;
                
                if (!WidgetProvider.ContainsWidget(zoneName, partKey))
                {
                    WidgetProvider.RegisterWidget(
                        zoneName, 
                        new HtmlWidgetInvoker(new HtmlString("<link rel=\"canonical\" href=\"{0}\" />".FormatInvariant(href))) 
                        { 
                            Key = partKey, 
                            Prepend = prepend 
                        });
                }
            }
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

            return new HtmlString(result.NullEmpty() ?? _seoSettings.GetLocalizedSetting(x => x.MetaDescription).Value);
        }

        public virtual IHtmlContent GetMetaKeywords()
        {
            string result = null;

            if (_metaKeywordParts != null)
            {
                result = string.Join(", ", _metaKeywordParts.Distinct(StringComparer.CurrentCultureIgnoreCase).Reverse().ToArray());
            }

            return new HtmlString(result.NullEmpty() ?? _seoSettings.GetLocalizedSetting(x => x.MetaKeywords).Value);
        }

        /// <summary>
        /// Given an app relative path for a static script or css file, tries to locate
        /// the minified version ([PathWithoutExtension].min.[Extension]) of this file in the same directory, but only if app
        /// runs in production mode. If a minified file is found, then its path is returned, otherwise
        /// <paramref name="path"/> is returned as is.
        /// </summary>
        /// <param name="path">File path to check a minified version for.</param>
        public virtual string TryFindMinFile(string path)
        {
            Guard.NotEmpty(path, nameof(path));
            
            if (!_appContext.HostEnvironment.IsDevelopment())
            {
                path = _minFiles.GetOrAdd(path, key =>
                {
                    try
                    {
                        if (!WebHelper.IsUrlLocalToHost(key))
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

                        var minPath = "{0}.min{1}".FormatInvariant(key.Substring(0, key.Length - extension.Length), extension);
                        if (_appContext.WebRoot.FileExists(minPath.TrimStart('~', '/')))
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
            }

            return _urlHelper.Value.Content(path);
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
