using System;
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
        const string LangForAttributeName = "language-attributes-for";
        const string BodyTagName = "body";
        const string TitleTagName = "title";

        [HtmlAttributeName(LangForAttributeName)]
        public Language LanguageAttributesFor { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (context.TagName == TitleTagName)
            {
                // Render meta accept-language right before the title tag
                var acceptLang = CultureInfo.CurrentUICulture.ToString();
                output.PreElement.AppendHtml(string.Format("<meta name=\"accept-language\" content=\"{0}\"/>", acceptLang));
            }
            else if (context.TagName == BodyTagName && !output.Attributes.ContainsName(DirAttributeName))
            {
                // Add dir attribute to body
                var isRtl = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
                var val = isRtl ? "rtl" : "ltr";
                output.Attributes.Add(DirAttributeName, val);
            }

            if (LanguageAttributesFor != null)
            {
                var code = LanguageAttributesFor.GetTwoLetterISOLanguageName();
                var rtl = LanguageAttributesFor.Rtl;

                output.MergeAttribute(LangAttributeName, code);
                output.MergeAttribute(DirAttributeName, rtl ? "rtl" : "ltr");
            }
        }
    }
}