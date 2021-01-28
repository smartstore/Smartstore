using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.UI.TagHelpers.Public
{
    [HtmlTargetElement("input", Attributes = ForAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    public class InputTagHelper : SmartTagHelper
    {
        const string ForAttributeName = "asp-for";

        protected override string GenerateTagId(TagHelperContext context)
            => null;
    }
}
