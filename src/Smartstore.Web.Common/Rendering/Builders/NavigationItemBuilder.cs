using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
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
            private set;
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

        public TBuilder HtmlAttributes(object attributes)
        {
            return HtmlAttributes(CommonHelper.ObjectToStringDictionary(attributes));
        }

        public TBuilder HtmlAttributes(IDictionary<string, string> attributes)
        {
            Item.HtmlAttributes.Clear();
            Item.HtmlAttributes.Merge(attributes);
            return (this as TBuilder);
        }

        public TBuilder LinkHtmlAttributes(object attributes)
        {
            return LinkHtmlAttributes(CommonHelper.ObjectToStringDictionary(attributes));
        }

        public TBuilder LinkHtmlAttributes(IDictionary<string, string> attributes)
        {
            Item.LinkHtmlAttributes.Clear();
            Item.LinkHtmlAttributes.Merge(attributes);
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

        public TBuilder Icon(string value)
        {
            Item.Icon = value;
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

    // TODO: (mh) (core) If this will be used is TBD

    //public abstract class NavigationItemtWithContentBuilder<TItem, TBuilder> : NavigationItemBuilder<TItem, TBuilder>
    //    where TItem : NavigationItemWithContent
    //    where TBuilder : NavigationItemtWithContentBuilder<TItem, TBuilder>
    //{

    //    public NavigationItemtWithContentBuilder(TItem item, HtmlHelper htmlHelper)
    //        : base(item)
    //    {
    //        Guard.NotNull(htmlHelper, nameof(htmlHelper));

    //        HtmlHelper = htmlHelper;
    //    }

    //    protected HtmlHelper HtmlHelper
    //    {
    //        get;
    //        private set;
    //    }

    //    /// <summary>
    //    /// Specifies whether the content should be loaded per AJAX into the content pane.
    //    /// </summary>
    //    /// <param name="value">value</param>
    //    /// <returns>builder</returns>
    //    /// <remarks>
    //    ///		This setting has no effect when no route is specified OR
    //    ///		static content was set.
    //    /// </remarks>
    //    public TBuilder Ajax(bool value = true)
    //    {
    //        Item.Ajax = value;
    //        return (this as TBuilder);
    //    }

    //    public TBuilder Content(string value)
    //    {
    //        if (value.IsEmpty())
    //        {
    //            // do nothing
    //            return (this as TBuilder);
    //        }
    //        return Content(x => new HelperResult(async writer => await writer.WriteAsync(value)));
    //    }

    //    public TBuilder Content(Func<dynamic, HelperResult> value)
    //    {
    //        return Content(value(null));
    //    }

    //    public TBuilder Content(HelperResult value)
    //    {
    //        Item.Content = value;
    //        return (this as TBuilder);
    //    }

    //    /// <summary>
    //    /// Renders child action as content
    //    /// </summary>
    //    /// <param name="action">Action name</param>
    //    /// <param name="controller">Controller name</param>
    //    /// <param name="routeValues">Route values</param>
    //    /// <returns>builder instance</returns>
    //    public TBuilder Content(string action, string controller, object routeValues)
    //    {
    //        return Content(action, controller, new RouteValueDictionary(routeValues));
    //    }

    //    /// <summary>
    //    /// Renders child action as content
    //    /// </summary>
    //    /// <param name="action">Action name</param>
    //    /// <param name="controller">Controller name</param>
    //    /// <param name="routeValues">Route values</param>
    //    /// <returns>builder instance</returns>
    //    public TBuilder Content(string action, string controller, RouteValueDictionary routeValues)
    //    {
    //        // TODO: (mh) (core) Invoke via viewcompoment.
    //        //return this.Content(x => new HelperResult(writer =>
    //        //{
    //        //    var value = this.HtmlHelper.Action(action, controller, routeValues);
    //        //    writer.Write(value);
    //        //}));

    //        return null;
    //    }

    //    public TBuilder ContentHtmlAttributes(object attributes)
    //    {
    //        return ContentHtmlAttributes(CommonHelper.ObjectToDictionary(attributes));
    //    }

    //    public TBuilder ContentHtmlAttributes(AttributeDictionary attributes)
    //    {
    //        Item.ContentHtmlAttributes.Clear();
    //        Item.ContentHtmlAttributes.Merge(attributes);
    //        return (this as TBuilder);
    //    }
    //}
}
