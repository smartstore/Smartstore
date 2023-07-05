using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        /// <summary>
        /// Appends a new tab item to the end of the current tabs collection.
        /// </summary>
        /// <param name="buildAction">Build action.</param>
        public Task AppendAsync(Action<TabItemBuilder> buildAction)
        {
            return CreateTagHelper(buildAction, null);
        }

        /// <summary>
        /// Prepends a new tab item to the current tabs collection.
        /// </summary>
        /// <param name="buildAction">Build action.</param>
        public Task PrependAsync(Action<TabItemBuilder> buildAction)
        {
            return CreateTagHelper(buildAction, 0);
        }

        /// <summary>
        /// Inserts a new tab at a given position.
        /// </summary>
        /// <param name="position">
        /// The position to insert the tab at. If value is negative, the new tab will be prepended.
        /// If value is larger than items count, tab will be appended.
        /// </param>
        /// <param name="buildAction">Build action.</param>
        private Task<TabTagHelper> InsertAtAsync(int position, Action<TabItemBuilder> buildAction)
        {
            return CreateTagHelper(buildAction, position);
        }

        /// <summary>
        /// Inserts a new tab after tab with given <paramref name="tabName"/>.
        /// If the adjacent tab does not exist, the new tab will be appended
        /// to the current tabs collection.
        /// </summary>
        /// <param name="tabName">Tab name to insert new tab after</param>
        /// <param name="buildAction">Build action.</param>
        public Task InsertAfterAsync(string tabName, Action<TabItemBuilder> buildAction)
        {
            Guard.NotEmpty(tabName, nameof(tabName));
            return InsertAfterAnyAsync(new[] { tabName }, buildAction);
        }

        /// <summary>
        /// Inserts a new tab after any tab which is contained in <paramref name="tabNames"/>.
        /// If adjacent tab does not exist, the new tab will be appended 
        /// to the current tabs collection.
        /// </summary>
        /// <param name="tabNames">Tab names to insert new tab after. First existing tab - from start to end - will be adjacent.</param>
        /// <param name="buildAction">Build action.</param>
        public Task InsertAfterAnyAsync(string[] tabNames, Action<TabItemBuilder> buildAction)
        {
            Guard.NotEmpty(tabNames, nameof(tabNames));

            int? position = -1;
            for (var i = 0; i < TabStrip.Tabs.Count; i++)
            {
                if (tabNames.Contains(TabStrip.Tabs[i].TabName, StringComparer.OrdinalIgnoreCase))
                {
                    position = i + 1;
                    break;
                }
            }

            if (position == -1)
            {
                position = null;
            }

            return CreateTagHelper(buildAction, position);
        }

        /// <summary>
        /// Inserts a new tab before tab with given <paramref name="tabName"/>.
        /// If the adjacent tab does not exist, the new tab will be appended 
        /// to the current tabs collection.
        /// </summary>
        /// <param name="tabName">Tab name to insert new tab before</param>
        /// <param name="buildAction">Build action.</param>
        public Task InsertBeforeAsync(string tabName, Action<TabItemBuilder> buildAction)
        {
            Guard.NotEmpty(tabName, nameof(tabName));
            return InsertBeforeAnyAsync(new[] { tabName }, buildAction);
        }

        /// <summary>
        /// Inserts a new tab before any tab which is contained in <paramref name="tabNames"/>.
        /// If adjacent tab does not exist, the new tab will be prepended 
        /// to the current tabs collection.
        /// </summary>
        /// <param name="tabNames">Tab names to insert new tab before. First existing tab - from start to end - will be adjacent.</param>
        /// <param name="buildAction">Build action.</param>
        public Task InsertBeforeAnyAsync(string[] tabNames, Action<TabItemBuilder> buildAction)
        {
            Guard.NotEmpty(tabNames, nameof(tabNames));

            int position = -1;
            for (var i = 0; i < TabStrip.Tabs.Count; i++)
            {
                if (tabNames.Contains(TabStrip.Tabs[i].TabName, StringComparer.OrdinalIgnoreCase))
                {
                    position = i;
                    break;
                }
            }

            return CreateTagHelper(buildAction, position);
        }

        private Task<TabTagHelper> CreateTagHelper(Action<TabItemBuilder> buildAction, int? position)
        {
            Guard.NotNull(buildAction, nameof(buildAction));

            var builder = new TabItemBuilder(new TabItem(), TabStrip.HtmlHelper);
            buildAction(builder);
            return ConvertToTagHelper(builder.AsItem(), position);
        }

        private async Task<TabTagHelper> ConvertToTagHelper(TabItem item, int? position)
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
                Position = position
            };

            if (item.IconLibrary == "bi" && tagHelper.Icon.HasValue())
            {
                tagHelper.Icon = tagHelper.Icon.EnsureStartsWith("bi:");
            }

            // Create TagHelperContext for tab passing it parent context's items dictionary (that's what Razor does)
            var context = new TagHelperContext("tab", new TagHelperAttributeList(), Context.Items, CommonHelper.GenerateRandomDigitCode(10));

            // Must init tab, otherwise "Tabs" list is empty inside tabstrip helper.
            tagHelper.Init(context);

            var outputAttrList = new TagHelperAttributeList();

            item.HtmlAttributes.CopyTo(outputAttrList);
            item.LinkHtmlAttributes.CopyTo(outputAttrList);

            if (!item.HasContent && item.HasRoute)
            {
                outputAttrList.Add("href", item.GenerateUrl(tagHelper.UrlHelper));
            }

            var output = new TagHelperOutput("tab", outputAttrList, async (_, _) =>
            {
                var content = item.HasContent
                    ? await item.GetContentAsync(tagHelper.ViewContext)
                    : HtmlString.Empty;

                var contentTag = new TagBuilder("div");
                contentTag.MergeAttributes(item.ContentHtmlAttributes, false);

                TagHelperContent tabContent = new DefaultTagHelperContent()
                    .AppendHtml(contentTag.Attributes.Count > 0 ? contentTag.RenderStartTag() : HtmlString.Empty)
                    .AppendHtml(content)
                    .AppendHtml(contentTag.Attributes.Count > 0 ? contentTag.RenderEndTag() : HtmlString.Empty);

                return tabContent;
            });

            // Process tab
            await tagHelper.ProcessAsync(context, output);

            return tagHelper;
        }
    }
}
