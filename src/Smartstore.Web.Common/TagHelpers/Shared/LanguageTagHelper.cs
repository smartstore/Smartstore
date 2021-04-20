using System.Globalization;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Localization;

namespace Smartstore.Web.TagHelpers.Shared
{
    [HtmlTargetElement("title")]
    [HtmlTargetElement("body")]
    [HtmlTargetElement("*", Attributes = LangForAttributeName)]
    public class LanguageTagHelper : TagHelper
    {
        const string DirAttributeName = "dir";
        const string LangAttributeName = "lang";
        const string LangForAttributeName = "sm-language-attributes-for";
        const string BodyTagName = "body";
        const string TitleTagName = "title";

        /// <summary>
        /// A <see cref="Language"/> or <see cref="LocalizedValue"/> instance.
        /// </summary>
        [HtmlAttributeName(LangForAttributeName)]
        public object LanguageAttributesFor { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (context.TagName == TitleTagName)
            {
                // Render meta accept-language right before the title tag
                var acceptLang = CultureInfo.CurrentUICulture.ToString();
                output.PreElement.AppendHtml($"<meta name='accept-language' content='{acceptLang}'/>");
            }
            else if (context.TagName == BodyTagName && !output.Attributes.ContainsName(DirAttributeName))
            {
                // Add dir attribute to body
                // TODO: (mc) (core) IsRightToLeft always returns false no matter what. Also this attribute was set on html-tag level prior.
                var isRtl = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
                var val = isRtl ? "rtl" : "ltr";
                output.Attributes.Add(DirAttributeName, val);
            }

            Language currentLanguage = null;

            if (LanguageAttributesFor is LocalizedValue localizedValue && localizedValue.BidiOverride)
            {
                currentLanguage = localizedValue.CurrentLanguage;
            }

            currentLanguage ??= LanguageAttributesFor as Language;

            if (currentLanguage != null)
            {
                var code = currentLanguage.GetTwoLetterISOLanguageName();
                var rtl = currentLanguage.Rtl;

                output.MergeAttribute(LangAttributeName, code);
                output.MergeAttribute(DirAttributeName, rtl ? "rtl" : "ltr");
            }
        }
    }
}