using System;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Localization;

namespace Smartstore.Web.UI.TagHelpers.Shared
{
    /// <summary>
    /// Tries to locate a script localization file and, if found, replaces {lang} token in src attribute.
    /// </summary>
    [HtmlTargetElement("script", Attributes = "src, " + LocaleForAttributeName)]
    public class LocalizationScriptTagHelper : TagHelper
    {
        const string LocaleForAttributeName = "asp-locale-for";
        const string LocaleFallbackAttributeName = "asp-locale-fallback";

        private readonly ILocalizationFileResolver _fileResolver;

        public LocalizationScriptTagHelper(ILocalizationFileResolver fileResolver)
        {
            _fileResolver = fileResolver;
        }

        // Must run BEFORE UrlResolutionTagHelper
        public override int Order => -3000;

        /// <summary>
        /// Tries to find a matching localization script file for the given language in the following order 
        /// (assuming language is 'de-DE', src attribute is 'lang-{lang}.js' and <see cref="FallbackCulture"/> is 'en-US'):
        /// <list type="number">
        ///		<item>Exact match > lang-de-DE.js</item>
        ///		<item>Neutral culture > lang-de.js</item>
        ///		<item>Any region for language > lang-de-CH.js</item>
        ///		<item>Exact match for fallback culture > lang-en-US.js</item>
        ///		<item>Neutral fallback culture > lang-en.js</item>
        ///		<item>Any region for fallback language > lang-en-GB.js</item>
        /// </list>
        /// <para>
        ///     The src attribute must contain the {lang} substitution token for locale code replacement.
        /// </para>
        /// <para>
        ///     The attribute charset="UTF-8" will be added if the script tag does not contain the charset attribute already.
        /// </para>
        /// <para>
        ///     Output will be suppressed if the directory does not exist or no localization file was found.
        /// </para>
        /// </summary>
        [HtmlAttributeName(LocaleForAttributeName)]
        public Language Language { get; set; }

        [HtmlAttributeName(LocaleFallbackAttributeName)]
        public string FallbackCulture { get; set; } = "en";

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var src = output.Attributes["src"].Value.ToString();

            if (src.Length < 11)
            {
                // Must be at least "~/{lang}.js"
                return;
            }

            var resolveResult = _fileResolver.Resolve(Language.UniqueSeoCode, src, true, FallbackCulture);

            if (resolveResult?.Success == true)
            {
                output.Attributes.SetAttribute("src", resolveResult.VirtualPath);
                output.MergeAttribute("charset", "UTF-8");
            }
            else
            {
                output.SuppressOutput();
            }
        }
    }
}
