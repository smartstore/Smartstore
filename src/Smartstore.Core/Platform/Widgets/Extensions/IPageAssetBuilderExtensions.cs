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
        /// Appends foot script files to the currently rendered page (rendered in zone <c>scripts</c>).
        /// </summary>
        /// <param name="urls">The urls to append.</param>
        public static void AppendScriptFiles(this IPageAssetBuilder builder, params string[] urls) => builder.AddScriptFiles(urls, AssetLocation.Foot, false);

        /// <summary>
        /// Prepends foot script files to the currently rendered page (rendered in zone <c>scripts</c>).
        /// </summary>
        /// <param name="urls">The urls to prepend.</param>
        public static void PrependScriptFiles(this IPageAssetBuilder builder, params string[] urls) => builder.AddScriptFiles(urls, AssetLocation.Foot, true);

        /// <summary>
        /// Appends head script files to the currently rendered page (rendered in zone <c>head_scripts</c>).
        /// </summary>
        /// <param name="urls">The urls to append.</param>
        public static void AppendHeadScriptFiles(this IPageAssetBuilder builder, params string[] urls) => builder.AddScriptFiles(urls, AssetLocation.Head, false);

        /// <summary>
        /// Prepends head script files to the currently rendered page (rendered in zone <c>head_scripts</c>).
        /// </summary>
        /// <param name="urls">The urls to prepend.</param>
        public static void PrependHeadScriptFiles(this IPageAssetBuilder builder, params string[] urls) => builder.AddScriptFiles(urls, AssetLocation.Head, true);

        /// <summary>
        /// Appends CSS files to the currently rendered page (rendered in zone <c>stylesheets</c>).
        /// </summary>
        /// <param name="urls">The urls to append.</param>
        public static void AppendCssFiles(this IPageAssetBuilder builder, params string[] urls) => builder.AddCssFiles(urls, false);

        /// <summary>
        /// Prepends CSS files to the currently rendered page (rendered in zone <c>stylesheets</c>).
        /// </summary>
        /// <param name="urls">The urls to prepend.</param>
        public static void PrependCssFiles(this IPageAssetBuilder builder, params string[] urls) => builder.AddCssFiles(urls, true);

        /// <summary>
        /// Adds a meta robots tag to the head.
        /// </summary>
        public static void AddMetaRobots(this IPageAssetBuilder builder, string name = "robots", string content = "noindex")
        {
            Guard.NotEmpty(name, nameof(name));
            Guard.NotEmpty(content, nameof(content));

            builder.AddHtmlContent(
                "head",
                new HtmlString($"<meta name='{name}' content='{content}' />"),
                $"meta_{name}");
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
                builder.AddHtmlContent(
                    zoneName,
                    new HtmlString("<link rel=\"canonical\" href=\"{0}\" />".FormatInvariant(href)),
                    href,
                    prepend);
            }
        }
    }
}
