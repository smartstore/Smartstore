using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Smartstore.Core.Content.Seo;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;

namespace Smartstore.Web.UI
{
    public partial class PageAssetBuilder : IPageAssetBuilder
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWidgetProvider _widgetProvider;
        private readonly SeoSettings _seoSettings;

        private List<string> _titleParts;
        private List<string> _metaDescriptionParts;
        private List<string> _metaKeywordParts;

        public PageAssetBuilder(
            IHttpContextAccessor httpContextAccessor,
            IWidgetProvider widgetProvider,
            SeoSettings seoSettings,
            IStoreContext storeContext)
        {
            _httpContextAccessor = httpContextAccessor;
            _widgetProvider = widgetProvider;
            _seoSettings = seoSettings;

            var htmlBodyId = storeContext.CurrentStore.HtmlBodyId;
            if (htmlBodyId.HasValue())
            {
                BodyAttributes["id"] = htmlBodyId;
            }
        }

        #region Build

        public AttributeDictionary RootAttributes { get; } = new();

        public AttributeDictionary BodyAttributes { get; } = new();

        public void PushTitleParts(IEnumerable<string> parts, bool append = false)
            => AddPartsCore(ref _titleParts, parts, append);

        public void PushMetaDescriptionParts(IEnumerable<string> parts, bool append = false)
            => AddPartsCore(ref _metaDescriptionParts, parts, append);

        public void PushMetaKeywordParts(IEnumerable<string> parts, bool append = false)
            => AddPartsCore(ref _metaKeywordParts, parts, append);

        public void PushCanonicalUrlParts(IEnumerable<string> parts, bool append = false)
        {
            const string zoneName = "head_canonical";
            
            if (parts == null || !parts.Any())
            {
                return;
            }

            foreach (var href in parts.Where(IsValidPart).Select(x => x.Trim()))
            {
                var partKey = "canonical:" + href;
                
                if (!_widgetProvider.ContainsWidget(zoneName, partKey))
                {
                    var tag = new TagBuilder("link");
                    tag.Attributes["rel"] = "canonical";
                    tag.Attributes["href"] = href;

                    _widgetProvider.RegisterWidget(zoneName, new HtmlWidgetInvoker(tag) { Key = partKey, Prepend = !append });
                }
            }
        }

        #endregion

        #region Resolve

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

        #endregion

        #region Utils

        // Helper func: changes all following public funcs to remove code redundancy
        private static void AddPartsCore<T>(ref List<T> list, IEnumerable<T> partsToAdd, bool append = false)
        {
            var parts = (partsToAdd ?? Enumerable.Empty<T>()).Where(IsValidPart);

            if (list == null)
            {
                list = new List<T>(parts);
            }
            else if (parts.Any())
            {
                if (append)
                {
                    // Appended elements must actually be prepended to the list, because
                    // outermost templates come last in rendering process.
                    // ---
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
