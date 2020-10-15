using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.Common.TagHelpers
{
    public class LanguageDirTagHelperComponent : TagHelperComponent
    {
        const string DirAttribute = "dir";

        private readonly HttpContext _httpContext;

        public LanguageDirTagHelperComponent(IHttpContextAccessor httpContextAccessor)
        {
            _httpContext = httpContextAccessor.HttpContext;
        }

        public override int Order => 1;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (string.Equals(context.TagName, "body", StringComparison.Ordinal) && !output.Attributes.ContainsName(DirAttribute))
            {
                var isRtl = _httpContext.Features.Get<IRequestCultureFeature>().RequestCulture.UICulture.TextInfo.IsRightToLeft;
                var val = isRtl ? "rtl" : "ltr";
                output.Attributes.Add(DirAttribute, val);
            }
        }
    }
}
