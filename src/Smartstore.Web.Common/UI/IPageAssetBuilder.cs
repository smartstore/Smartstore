using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Smartstore.Core.Content.Seo;

namespace Smartstore.Web.UI
{
    public partial interface IPageAssetBuilder
    {
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
        void PushTitleParts(IEnumerable<string> parts, bool append = false);

        /// <summary>
        /// Pushes meta description parts to the currently rendered page.
        /// </summary>
        /// <param name="parts">The parts to push.</param>
        void PushMetaDescriptionParts(IEnumerable<string> parts, bool append = false);

        /// <summary>
        /// Pushes meta keyword parts to the currently rendered page.
        /// </summary>
        /// <param name="parts">The parts to push.</param>
        void PushMetaKeywordParts(IEnumerable<string> parts, bool append = false);

        /// <summary>
        /// Pushes canonical url parts to the currently rendered page.
        /// </summary>
        /// <param name="parts">The parts to push.</param>
        void PushCanonicalUrlParts(IEnumerable<string> parts, bool append = false);

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
    }
}
