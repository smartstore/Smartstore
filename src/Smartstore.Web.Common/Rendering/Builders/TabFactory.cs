using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Content.Menus;
using Smartstore.Utilities;
using Smartstore.Web.TagHelpers.Shared;

namespace Smartstore.Web.Rendering.Builders
{
    public class TabFactory
    {
        public TabFactory(TabStripTagHelper tabStrip, TagHelperContext context)
        {
            Guard.NotNull(tabStrip, nameof(tabStrip));
            Guard.NotNull(context, nameof(context));

            TabStrip = tabStrip;
            Context = context;
        }

        internal TabStripTagHelper TabStrip { get; }
        internal TagHelperContext Context { get; }

        public async Task AddAsync(Action<TabItemBuilder> buildAction)
        {
            await CreateTagHelper(buildAction);
        }

        private Task<TabTagHelper> CreateTagHelper(Action<TabItemBuilder> buildAction)
        {
            Guard.NotNull(buildAction, nameof(buildAction));

            var item = new TabItem();
            buildAction(new TabItemBuilder(item, TabStrip.HtmlHelper));
            return ConvertToTagHelper(item);
        }

        private async Task<TabTagHelper> ConvertToTagHelper(TabItem item)
        {
            var tagHelper = new TabTagHelper
            {
                ViewContext = TabStrip.ViewContext,
                Selected = item.Selected,
                Disabled = !item.Enabled,
                Visible = item.Visible,
                Ajax = item.Ajax,
                Title = item.Text,
                Name = item.Name,
                BadgeStyle = (BadgeStyle)item.BadgeStyle,
                BadgeText = item.BadgeText,
                Icon = item.Icon,
                ImageUrl = item.ImageUrl,
                Summary = item.Summary
            };
            
            // Id, Attrs, Content, TabInnerContent

            // Create TagHelperContext for tab passing it parent context's items dictionary (that's what Razor does)
            var context = new TagHelperContext("tab", new TagHelperAttributeList(), Context.Items, CommonHelper.GenerateRandomDigitCode(10));

            // Must init tab, otherwise "Tabs" list is empty inside tabstrip helper.
            tagHelper.Init(context);

            var outputAttrList = new TagHelperAttributeList();

            if (!item.HasContent && item.HasRoute)
            {
                outputAttrList.Add("href", item.GenerateUrl(tagHelper.UrlHelper));
            }

            item.HtmlAttributes.CopyTo(outputAttrList);

            var output = new TagHelperOutput("tab", outputAttrList, async (_, _) =>
            {
                TagHelperContent tabContent = new DefaultTagHelperContent();

                if (item.HasContent)
                {
                    tabContent.SetHtmlContent(await item.GetContentAsync(tagHelper.ViewContext));
                }

                return tabContent;
            });

            // Process tab
            await tagHelper.ProcessAsync(context, output);

            return tagHelper;
        }
    }
}
