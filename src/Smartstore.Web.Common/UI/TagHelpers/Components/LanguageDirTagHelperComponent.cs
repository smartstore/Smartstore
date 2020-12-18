using System;
using System.Globalization;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.UI.TagHelpers
{
    public class LanguageDirTagHelperComponent : TagHelperComponent
    {
        const string DirAttribute = "dir";

        public override int Order => 1;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (context.TagName == "body" && !output.Attributes.ContainsName(DirAttribute))
            {
                var isRtl = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
                var val = isRtl ? "rtl" : "ltr";
                output.Attributes.Add(DirAttribute, val);
            }
        }
    }
}