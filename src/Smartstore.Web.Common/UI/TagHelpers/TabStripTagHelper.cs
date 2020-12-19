using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.UI.TagHelpers
{
    [OutputElementHint("div")]
    [RestrictChildren("tab")]
    [HtmlTargetElement("tabstrip")]
    public class TabStripTagHelper : SmartTagHelper
    {
        [HtmlAttributeNotBound]
        public List<TabTagHelper> Tabs { get; set; } = new();

        public override void Init(TagHelperContext context)
        {
            base.Init(context);
            context.Items["TabStrip"] = this;
        }

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            var tabs = Tabs;
            await output.GetChildContentAsync();
            tabs = Tabs;
        }
    }
}
