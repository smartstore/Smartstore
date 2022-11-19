using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Content.Menus;
using Smartstore.Utilities;

namespace Smartstore.Web.Rendering.Builders
{
    public abstract class NavigationItemtWithContentBuilder<TItem, TBuilder> : NavigationItemBuilder<TItem, TBuilder>, IHideObjectMembers
        where TItem : NavigationItemWithContent
        where TBuilder : NavigationItemtWithContentBuilder<TItem, TBuilder>
    {
        public NavigationItemtWithContentBuilder(TItem item, IHtmlHelper htmlHelper)
            : base(item)
        {
            Guard.NotNull(htmlHelper, nameof(htmlHelper));
            HtmlHelper = htmlHelper;
        }

        protected IHtmlHelper HtmlHelper { get; }

        /// <summary>
        /// Specifies whether the content should be loaded per AJAX into the content pane.
        /// </summary>
        /// <param name="value">value</param>
        /// <returns>builder</returns>
        /// <remarks>
        ///		This setting has no effect when no route is specified OR
        ///		static content was set.
        /// </remarks>
        public TBuilder Ajax(bool value = true)
        {
            Item.Ajax = value;
            return (this as TBuilder);
        }

        public TBuilder Content(string value)
        {
            if (value.IsEmpty())
            {
                // do nothing
                return (this as TBuilder);
            }

            return Content(new HtmlString(value));
        }

        public TBuilder Content(IHtmlContent value)
        {
            Item.Content = value;
            return (this as TBuilder);
        }

        public TBuilder Content(Widget value)
        {
            Item.Widget = value;
            return (this as TBuilder);
        }

        public TBuilder ContentHtmlAttributes(string name, string value, bool condition = true)
        {
            if (condition) Item.ContentHtmlAttributes.Merge(name, value);
            return (this as TBuilder);
        }

        public TBuilder ContentHtmlAttributes(object attributes)
        {
            return ContentHtmlAttributes(ConvertUtility.ObjectToStringDictionary(attributes));
        }

        public TBuilder ContentHtmlAttributes(IDictionary<string, string> attributes)
        {
            Item.ContentHtmlAttributes.Merge(attributes);
            return (this as TBuilder);
        }
    }
}