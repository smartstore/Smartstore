using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Widgets;

namespace Smartstore.Web.TagHelpers.Shared
{
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
		const string ZoneNameAttributeName = "sm-zone-name";
		const string ReplaceContentAttributeName = "sm-replace-content";
		const string RemoveIfEmptyAttributeName = "sm-remove-if-empty";

		private readonly IWidgetSelector _widgetSelector;

		public ZoneTagHelper(IWidgetSelector widgetSelector)
		{
			_widgetSelector = widgetSelector;
		}

		[HtmlAttributeName(ZoneNameAttributeName)]
		public string ZoneName { get; set; }

		/// <summary>
		/// Specifies whether any default zone content should be removed if at least one 
		/// widget is rendered in the zone.
		/// </summary>
		[HtmlAttributeName(ReplaceContentAttributeName)]
		public bool ReplaceContent { get; set; }

		/// <summary>
		/// Whether to remove the root zone tag when it has no content. 
		/// Only applies to HTML tags like div, span, section etc..
		/// <c>zone</c> tags are always removed.
		/// </summary>
		[HtmlAttributeName(RemoveIfEmptyAttributeName)]
		public bool RemoveIfEmpty { get; set; }

		protected override string GenerateTagId(TagHelperContext context) => null;

		protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
			var isHtmlTag = output.TagName != "zone";

			var widgets = await _widgetSelector.GetWidgetsAsync(ZoneName, ViewContext.ViewData.Model);

			if (!isHtmlTag)
			{
				// Never render <zone> tag
				output.TagName = null;
			}

			if (widgets.Any())
            {
				if (ReplaceContent)
				{
					output.Content.SetContent(string.Empty);
				}

				foreach (var widget in widgets)
				{
					var target = widget.Prepend ? output.PreContent : output.PostContent;
					target.AppendHtml(await widget.InvokeAsync(ViewContext));
				}
			}
			else
            {
				// No widgets
				if (RemoveIfEmpty && output.TagName.HasValue())
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