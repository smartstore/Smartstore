using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Content.Menus;

namespace Smartstore.Web.Rendering.Builders
{
    public class TabItem : NavigationItemWithContent
    {
        public TabItem()
        {
            Visible = true;
        }

        public string Name { get; set; }
    }

    public class TabItemBuilder : NavigationItemtWithContentBuilder<TabItem, TabItemBuilder>
    {
        public TabItemBuilder(TabItem item, IHtmlHelper htmlHelper)
            : base(item, htmlHelper)
        {
        }

        /// <summary>
        /// Unique name of tab item.
        /// </summary>
        public TabItemBuilder Name(string value)
        {
            Item.Name = value;
            return this;
        }

        public static implicit operator TabItem(TabItemBuilder builder)
            => builder.AsItem();
    }
}
