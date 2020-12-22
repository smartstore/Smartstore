using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.UI.TagHelpers
{
	/// <summary>
	/// Content injection modes.
	/// </summary>
	public enum ZoneInjectMode
	{
		/// <summary>
		/// Inserts injected content after existing content.
		/// </summary>
		Append,

		/// <summary>
		/// Inserts injected content before existing content.
		/// </summary>
		Prepend,

		/// <summary>
		/// Replaces existing with injected content.
		/// </summary>
		Replace
	}

	[HtmlTargetElement("zone", Attributes = ZoneNameAttributeName)]
	[HtmlTargetElement("div", Attributes = ZoneNameAttributeName)]
	[HtmlTargetElement("span", Attributes = ZoneNameAttributeName)]
	[HtmlTargetElement("p", Attributes = ZoneNameAttributeName)]
	[HtmlTargetElement("section", Attributes = ZoneNameAttributeName)]
	[HtmlTargetElement("aside", Attributes = ZoneNameAttributeName)]
	[HtmlTargetElement("header", Attributes = ZoneNameAttributeName)]
	[HtmlTargetElement("footer", Attributes = ZoneNameAttributeName)]
	public class ZoneTagHelper : SmartTagHelper
    {
		const string ZoneNameAttributeName = "zone-name";

		private readonly IWidgetSelector _widgetSelector;

		public ZoneTagHelper(IWidgetSelector widgetSelector)
        {
			_widgetSelector = widgetSelector;
        }

		[HtmlAttributeName(ZoneNameAttributeName)]
		public string ZoneName { get; set; }

		/// <summary>
		/// Whether to remove the root zone tag when it has no content. 
		/// Only applies to HTML tags like div, span, section etc..
		/// <c>zone</c> tags are always removed.
		/// </summary>
		public bool RemoveWhenEmpty { get; set; }

		/// <summary>
		/// Specifies how content should be injected if zone contains default content. Default is <see cref="ZoneInjectMode.Append"/>.
		/// </summary>
		public ZoneInjectMode? InjectMode { get; set; }

		protected override string GenerateTagId(TagHelperContext context) => null;

		protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
			var isHtmlTag = output.TagName != "zone";

			var widgets = _widgetSelector.GetWidgets(ZoneName, ViewContext.ViewData.Model);

			if (!isHtmlTag)
			{
				// Never render <zone> tag
				output.TagName = null;
			}

			if (widgets.Any())
            {
				var injectMode = InjectMode ?? ZoneInjectMode.Append;
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
			else
            {
				// No widgets
				if (RemoveWhenEmpty && output.TagName.HasValue())
                {
					var childContent = await output.GetChildContentAsync();
					if (childContent.IsEmptyOrWhiteSpace)
                    {
						output.TagName = null;
                    }
				}
            }
		}
    }
}