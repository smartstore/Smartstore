using Microsoft.AspNetCore.Routing;
using Smartstore.Core.Content.Menus;
using Smartstore.Utilities;

namespace Smartstore.Web.Rendering.Builders
{
    public abstract class NavigationItemBuilder<TItem, TBuilder> : IHideObjectMembers
        where TItem : NavigationItem
        where TBuilder : NavigationItemBuilder<TItem, TBuilder>
    {
        protected NavigationItemBuilder(TItem item)
        {
            Guard.NotNull(item, nameof(item));

            Item = item;
        }

        protected internal TItem Item
        {
            get;
            internal set;
        }

        public TBuilder Action(RouteValueDictionary routeValues)
        {
            Item.Action(routeValues);
            return (this as TBuilder);
        }

        public TBuilder Action(string actionName)
        {
            return Action(actionName, null, null);
        }

        public TBuilder Action(string actionName, object routeValues)
        {
            return Action(actionName, null, routeValues);
        }

        public TBuilder Action(string actionName, RouteValueDictionary routeValues)
        {
            return Action(actionName, null, routeValues);
        }

        public TBuilder Action(string actionName, string controllerName)
        {
            return Action(actionName, controllerName, null);
        }

        public TBuilder Action(string actionName, string controllerName, object routeValues)
        {
            Item.Action(actionName, controllerName, routeValues);
            return (this as TBuilder);
        }

        public TBuilder Action(string actionName, string controllerName, RouteValueDictionary routeValues)
        {
            Item.Action(actionName, controllerName, routeValues);
            return (this as TBuilder);
        }

        public TBuilder Route(string routeName)
        {
            return Route(routeName, null);
        }

        public TBuilder Route(string routeName, object routeValues)
        {
            Item.Route(routeName, routeValues);
            return (this as TBuilder);
        }

        public TBuilder Route(string routeName, RouteValueDictionary routeValues)
        {
            Item.Route(routeName, routeValues);
            return (this as TBuilder);
        }

        public TBuilder QueryParam(string paramName, params string[] booleanParamNames)
        {
            Item.ModifyParam(paramName, booleanParamNames);
            return (this as TBuilder);
        }

        public TBuilder Url(string value)
        {
            Item.Url(value);
            return (this as TBuilder);
        }

        public TBuilder HtmlAttributes(string name, string value, bool condition = true)
        {
            if (condition) Item.HtmlAttributes.Merge(name, value);
            return (this as TBuilder);
        }

        public TBuilder HtmlAttributes(object attributes)
        {
            return HtmlAttributes(ConvertUtility.ObjectToStringDictionary(attributes));
        }

        public TBuilder HtmlAttributes(IDictionary<string, string> attributes)
        {
            Item.HtmlAttributes.Merge(attributes);
            return (this as TBuilder);
        }

        public TBuilder LinkHtmlAttributes(string name, string value, bool condition = true)
        {
            if (condition) Item.LinkHtmlAttributes.Merge(name, value);
            return (this as TBuilder);
        }

        public TBuilder LinkHtmlAttributes(object attributes)
        {
            return LinkHtmlAttributes(ConvertUtility.ObjectToStringDictionary(attributes));
        }

        public TBuilder LinkHtmlAttributes(IDictionary<string, string> attributes)
        {
            Item.LinkHtmlAttributes.Merge(attributes);
            return (this as TBuilder);
        }

        public TBuilder BadgeHtmlAttributes(string name, string value, bool condition = true)
        {
            if (condition) Item.BadgeHtmlAttributes.Merge(name, value);
            return (this as TBuilder);
        }

        public TBuilder BadgeHtmlAttributes(object attributes)
        {
            return BadgeHtmlAttributes(ConvertUtility.ObjectToStringDictionary(attributes));
        }

        public TBuilder BadgeHtmlAttributes(IDictionary<string, string> attributes)
        {
            Item.BadgeHtmlAttributes.Merge(attributes);
            return (this as TBuilder);
        }

        public TBuilder ImageUrl(string value)
        {
            Item.ImageUrl = value;
            return (this as TBuilder);
        }

        public TBuilder ImageId(int? value)
        {
            Item.ImageId = value;
            return (this as TBuilder);
        }

        public TBuilder Icon(string value, string libary = null)
        {
            Item.IconLibrary = libary;
            Item.Icon = value;
            return (this as TBuilder);
        }

        public TBuilder IconClass(string value)
        {
            Item.IconClass = value;
            return (this as TBuilder);
        }

        public TBuilder Text(string value)
        {
            Item.Text = value;
            return (this as TBuilder);
        }

        public TBuilder Summary(string value)
        {
            Item.Summary = value;
            return (this as TBuilder);
        }

        public TBuilder Badge(string value, BadgeStyle style = BadgeStyle.Secondary, bool condition = true)
        {
            if (condition)
            {
                Item.BadgeText = value;
                Item.BadgeStyle = (int)style;
            }
            return (this as TBuilder);
        }

        public TBuilder Visible(bool value)
        {
            Item.Visible = value;
            return (this as TBuilder);
        }

        public TBuilder Encoded(bool value)
        {
            Item.Encoded = value;
            return (this as TBuilder);
        }

        public TBuilder Selected(bool value)
        {
            Item.Selected = value;
            return (this as TBuilder);
        }

        public TBuilder Enabled(bool value)
        {
            Item.Enabled = value;
            return (this as TBuilder);
        }

        public TItem AsItem() => Item;
    }
}
