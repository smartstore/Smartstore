using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.UI.TagHelpers
{
	[HtmlTargetElement("zone")]
	public class ZoneTagHelper : SmartTagHelper
    {
		private readonly IWidgetSelector _widgetSelector;

		public ZoneTagHelper(IWidgetSelector widgetSelector)
        {
			_widgetSelector = widgetSelector;
        }

		[HtmlAttributeName("name")]
		public string Name { get; set; }

		/// <summary>
		/// Specifies how content should be injected if zone contains default content. Default is <see cref="ZoneInjectMode.Replace"/>.
		/// </summary>
		[HtmlAttributeName("inject-mode")]
		public ZoneInjectMode? InjectMode { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
			output.TagName = null;

			if (Name.IsEmpty())
            {
				return;
			}

			var widgets = _widgetSelector.GetWidgets(Name, ViewContext.ViewData.Model);

			if (widgets.Any())
            {	
				var injectMode = InjectMode ?? ZoneInjectMode.Replace;

				if (injectMode == ZoneInjectMode.Prepend)
				{
					foreach (var widget in widgets.Reverse())
					{
						output.PreContent.SetHtmlContent(await widget.InvokeAsync(ViewContext));
					}
				}
				else
				{
					if (injectMode == ZoneInjectMode.Replace)
                    {
						output.SuppressOutput();
					}

					foreach (var widget in widgets)
					{
						output.PostContent.SetHtmlContent(await widget.InvokeAsync(ViewContext));
					}
				}
			}
		}
    }
}