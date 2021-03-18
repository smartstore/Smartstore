using System;
using System.Linq;
using Microsoft.AspNetCore.Html;

namespace Smartstore.Core.Widgets
{
    public static class IPageAssetBuilderExtensions
    {
        /// <summary>
        /// Appends document title parts to the currently rendered page.
        /// </summary>
        /// <param name="parts">The parts to append.</param>
        public static void AppendTitleParts(this IPageAssetBuilder builder, params string[] parts) => builder.AddTitleParts(parts, false);

        /// <summary>
        /// Prepends document title parts to the currently rendered page.
        /// </summary>
        /// <param name="parts">The parts to prepend.</param>
        public static void PrependTitleParts(this IPageAssetBuilder builder, params string[] parts) => builder.AddTitleParts(parts, true);

        /// <summary>
        /// Appends meta description parts to the currently rendered page.
        /// </summary>
        /// <param name="parts">The parts to append.</param>
        public static void AppendMetaDescriptionParts(this IPageAssetBuilder builder, params string[] parts) => builder.AddMetaDescriptionParts(parts, false);

        /// <summary>
        /// Prepends meta description parts to the currently rendered page.
        /// </summary>
        /// <param name="parts">The parts to prepend.</param>
        public static void PrependMetaDescriptionParts(this IPageAssetBuilder builder, params string[] parts) => builder.AddMetaDescriptionParts(parts, true);

        /// <summary>
        /// Appends meta keyword parts to the currently rendered page.
        /// </summary>
        /// <param name="parts">The parts to append.</param>
        public static void AppendMetaKeywordsParts(this IPageAssetBuilder builder, params string[] parts) => builder.AddMetaKeywordParts(parts, false);

        /// <summary>
        /// Prepends meta keyword parts to the currently rendered page.
        /// </summary>
        /// <param name="parts">The parts to prepend.</param>
        public static void PrependMetaKeywordsParts(this IPageAssetBuilder builder, params string[] parts) => builder.AddMetaKeywordParts(parts, true);

        /// <summary>
        /// Appends canonical url parts to the currently rendered page (rendered in zone <c>head_canonical</c>).
        /// </summary>
        /// <param name="parts">The parts to append.</param>
        public static void AppendCanonicalUrlParts(this IPageAssetBuilder builder, params string[] parts) => AddCanonicalUrlPartsInternal(builder, false, parts);

        /// <summary>
        /// Prepends canonical url parts to the currently rendered page (rendered in zone <c>head_canonical</c>).
        /// </summary>
        /// <param name="parts">The parts to prepend.</param>
        public static void PrependCanonicalUrlParts(this IPageAssetBuilder builder, params string[] parts) => AddCanonicalUrlPartsInternal(builder, true, parts);

        /// <summary>
        /// Appends foot script file parts to the currently rendered page (rendered in zone <c>scripts</c>).
        /// </summary>
        /// <param name="parts">The parts to append.</param>
        public static void AppendScriptParts(this IPageAssetBuilder builder, params string[] parts) => AddScriptPartsInternal(builder, AssetLocation.Foot, false, parts);

        /// <summary>
        /// Prepends foot script file parts to the currently rendered page (rendered in zone <c>scripts</c>).
        /// </summary>
        /// <param name="parts">The parts to prepend.</param>
        public static void PrependScriptParts(this IPageAssetBuilder builder, params string[] parts) => AddScriptPartsInternal(builder, AssetLocation.Foot, true, parts);

        /// <summary>
        /// Appends head script file parts to the currently rendered page (rendered in zone <c>head_scripts</c>).
        /// </summary>
        /// <param name="parts">The parts to append.</param>
        public static void AppendHeadScriptParts(this IPageAssetBuilder builder, params string[] parts) => AddScriptPartsInternal(builder, AssetLocation.Head, false, parts);

        /// <summary>
        /// Prepends head script file parts to the currently rendered page (rendered in zone <c>head_scripts</c>).
        /// </summary>
        /// <param name="parts">The parts to prepend.</param>
        public static void PrependHeadScriptParts(this IPageAssetBuilder builder, params string[] parts) => AddScriptPartsInternal(builder, AssetLocation.Head, true, parts);

        /// <summary>
        /// Appends CSS file parts to the currently rendered page (rendered in zone <c>head_stylesheets</c>).
        /// </summary>
        /// <param name="parts">The parts to append.</param>
        public static void AppendCssFileParts(this IPageAssetBuilder builder, params string[] parts) => AddCssFilePartsInternal(builder, false, parts);

        /// <summary>
        /// Prepends CSS file parts to the currently rendered page (rendered in zone <c>head_stylesheets</c>).
        /// </summary>
        /// <param name="parts">The parts to prepend.</param>
        public static void PrependCssFileParts(this IPageAssetBuilder builder, params string[] parts) => AddCssFilePartsInternal(builder, true, parts);

        /// <summary>
        /// Adds a meta robots tag to the head.
        /// </summary>
        public static void AddMetaRobots(this IPageAssetBuilder builder, string name = "robots", string content = "noindex")
        {
            Guard.NotEmpty(name, nameof(name));
            Guard.NotEmpty(content, nameof(content));

            var key = "meta_" + name + '_' + content;
            AddHtmlContent(builder, 
                "head",
                new HtmlString("<meta name=\"{0}\" content=\"{1}\" />".FormatInvariant(name, content)),
                key);
        }

        /// <summary>
        /// Adds custom html content to a target zone.
        /// </summary>
        /// <param name="targetZone">The zone name to render <paramref name="content"/> in.</param>
        /// <param name="content">The html content to render.</param>
        /// <param name="key">An optional key to ensure uniqueness within the target zone.</param>
        /// <param name="prepend"><c>true</c> renders the <paramref name="content"/> before any zone content.</param>
        public static void AddHtmlContent(this IPageAssetBuilder builder, string targetZone, IHtmlContent content, string key = null, bool prepend = false)
        {
            Guard.NotEmpty(targetZone, nameof(targetZone));
            Guard.NotNull(content, nameof(content));

            if (key.HasValue() && builder.WidgetProvider.ContainsWidget(targetZone, key))
            {
                return;
            }

            builder.WidgetProvider.RegisterWidget(
                targetZone,
                new HtmlWidgetInvoker(content) { Key = key, Prepend = prepend });
        }

        private static void AddCanonicalUrlPartsInternal(IPageAssetBuilder builder, bool prepend, params string[] parts)
        {
            const string zoneName = "head_canonical";

            if (parts.Length == 0)
            {
                return;
            }

            foreach (var href in parts.Select(x => x.Trim()))
            {
                AddHtmlContent(builder,
                    zoneName,
                    new HtmlString("<link rel=\"canonical\" href=\"{0}\" />".FormatInvariant(href)),
                    href,
                    prepend);
            }
        }

        private static void AddScriptPartsInternal(IPageAssetBuilder builder, AssetLocation location, bool prepend, params string[] parts)
        {
            if (parts.Length == 0)
            {
                return;
            }

            string zoneName = location == AssetLocation.Head ? "head_scripts" : "scripts";

            foreach (var src in parts.Select(x => x.Trim()))
            {
                AddHtmlContent(builder,
                    zoneName,
                    new HtmlString("<script src=\"{0}\"></script>".FormatInvariant(builder.TryFindMinFile(src))),
                    src,
                    prepend);
            }
        }

        private static void AddCssFilePartsInternal(IPageAssetBuilder builder, bool prepend, params string[] parts)
        {
            const string zoneName = "head_stylesheets";

            if (parts.Length == 0)
            {
                return;
            }

            foreach (var href in parts.Select(x => x.Trim()))
            {
                AddHtmlContent(builder,
                    zoneName,
                    new HtmlString("<link href=\"{0}\" rel=\"stylesheet\" type=\"text/css\" />".FormatInvariant(builder.TryFindMinFile(href))),
                    href,
                    prepend);
            }
        }
    }
}
