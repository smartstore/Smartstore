using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.FileProviders;
using Smartstore.Core.Seo;

namespace Smartstore.Core.Widgets
{
    public partial interface IPageAssetBuilder
    {
        IWidgetProvider WidgetProvider { get; }

        /// <summary>
        /// Gets the root element (html tag) attribute dictionary.
        /// </summary>
        AttributeDictionary RootAttributes { get; }

        /// <summary>
        /// Gets the body tag attribute dictionary.
        /// </summary>
        AttributeDictionary BodyAttributes { get; }

        /// <summary>
        /// Pushes document title parts to the currently rendered page.
        /// </summary>
        /// <param name="parts">The parts to push.</param>
        /// <param name="prepend"><c>true</c> to insert <paramref name="parts"/> at the beginning of the current parts list.</param>
        void AddTitleParts(IEnumerable<string> parts, bool prepend = false);

        /// <summary>
        /// Pushes meta description parts to the currently rendered page.
        /// </summary>
        /// <param name="parts">The parts to push.</param>
        /// <param name="prepend"><c>true</c> to insert <paramref name="parts"/> at the beginning of the current parts list.</param>
        void AddMetaDescriptionParts(IEnumerable<string> parts, bool prepend = false);

        /// <summary>
        /// Pushes meta keyword parts to the currently rendered page.
        /// </summary>
        /// <param name="parts">The parts to push.</param>
        /// <param name="prepend"><c>true</c> to insert <paramref name="parts"/> at the beginning of the current parts list.</param>
        void AddMetaKeywordParts(IEnumerable<string> parts, bool prepend = false);

        /// <summary>
        /// Adds script files to the currently rendered page.
        /// </summary>
        /// <param name="urls">The file urls to add.</param>
        /// <param name="prepend"><c>true</c> to insert <paramref name="urls"/> at the beginning of the current list.</param>
        void AddScriptFiles(IEnumerable<string> urls, AssetLocation location, bool prepend = false);

        /// <summary>
        /// Adds css files to the currently rendered page.
        /// </summary>
        /// <param name="urls">The file urls to add.</param>
        /// <param name="prepend"><c>true</c> to insert <paramref name="urls"/> at the beginning of the current list.</param>
        void AddCssFiles(IEnumerable<string> urls, bool prepend = false);

        /// <summary>
        /// Adds custom html content to a target zone.
        /// </summary>
        /// <param name="targetZone">The zone name to render <paramref name="content"/> in.</param>
        /// <param name="content">The html content to render.</param>
        /// <param name="key">An optional key to ensure uniqueness within the target zone.</param>
        /// <param name="prepend"><c>true</c> renders the <paramref name="content"/> before any zone content.</param>
        void AddHtmlContent(string targetZone, IHtmlContent content, string key = null, bool prepend = false);

        /// <summary>
        /// Gets the document title which is composed of all current title parts
        /// separated by <see cref="SeoSettings.PageTitleSeparator"/>
        /// </summary>
        /// <param name="addDefaultTitle">
        /// Appends or prepends <see cref="SeoSettings.MetaTitle"/> according to
        /// <see cref="SeoSettings.PageTitleSeoAdjustment"/>. Separates both parts
        /// with <see cref="SeoSettings.PageTitleSeparator"/>.
        /// </param>
        /// <returns>Document title.</returns>
        IHtmlContent GetDocumentTitle(bool addDefaultTitle);

        /// <summary>
        /// Gets the document meta description which is composed of all current description parts separated by ", ".
        /// </summary>
        IHtmlContent GetMetaDescription();

        /// <summary>
        /// Gets the document meta keywords which is composed of all current keyword parts separated by ", ".
        /// </summary>
        IHtmlContent GetMetaKeywords();

        /// <summary>
        /// Given an app relative path for a static script or css file, tries to locate
        /// the minified version ([PathWithoutExtension].min.[Extension]) of this file in the same directory, but only if app
        /// runs in production mode. If a minified file is found, then its path is returned, otherwise
        /// <paramref name="path"/> is returned as is.
        /// </summary>
        /// <param name="path">File path to check a minified version for.</param>
        /// <param name="fileProvider">File provider to use for file resolution or <c>null</c> to use WebRoot file provider.</param>
        string TryFindMinFile(string path, IFileProvider fileProvider = null);
    }
}
