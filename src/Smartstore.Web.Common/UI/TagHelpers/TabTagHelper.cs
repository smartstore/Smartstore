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
    [HtmlTargetElement("tab", ParentTag = "tabstrip")]
    public class TabTagHelper : SmartTagHelper
    {
        public override void Init(TagHelperContext context)
        {
            base.Init(context);
            if (context.Items.TryGetValue("TabStrip", out var obj) && obj is TabStripTagHelper parent)
            {
                parent.Tabs.Add(this);
            }
        }
    }
}
