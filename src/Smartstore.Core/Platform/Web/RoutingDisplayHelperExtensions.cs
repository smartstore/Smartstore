using System;
using Microsoft.AspNetCore.Routing;

namespace Smartstore
{
    public static class RoutingDisplayHelperExtensions
    {
        public static bool IsHomePage(this IDisplayHelper displayHelper)
        {
            return displayHelper.HttpContext.GetItem(nameof(IsHomePage), () =>
            {
                var routeValues = displayHelper.HttpContext.GetRouteData().Values;
                var response = displayHelper.HttpContext.Response;

                return response.StatusCode != 404 &&
                    routeValues.GetControllerName().EqualsNoCase("home") &&
                    routeValues.GetActionName().EqualsNoCase("index");
            });
        }

        public static string CurrentPageType(this IDisplayHelper displayHelper)
            => IdentifyPage(displayHelper).CurrentPageType;

        public static object CurrentPageId(this IDisplayHelper displayHelper)
            => IdentifyPage(displayHelper).CurrentPageId;

        public static int CurrentCategoryId(this IDisplayHelper displayHelper)
            => displayHelper.CurrentPageType() == "category" ? displayHelper.CurrentPageId().Convert<int>() : 0;

        public static int CurrentManufacturerId(this IDisplayHelper displayHelper)
            => displayHelper.CurrentPageType() == "manufacturer" ? displayHelper.CurrentPageId().Convert<int>() : 0;

        public static int CurrentProductId(this IDisplayHelper displayHelper)
            => displayHelper.CurrentPageType() == "product" ? displayHelper.CurrentPageId().Convert<int>() : 0;

        public static int CurrentTopicId(this IDisplayHelper displayHelper)
            => displayHelper.CurrentPageType() == "topic" ? displayHelper.CurrentPageId().Convert<int>() : 0;

        private static PageIdentity IdentifyPage(IDisplayHelper displayHelper)
        {
            return displayHelper.HttpContext.GetItem("PageIdentity", () => 
            {
                var context = displayHelper.HttpContext;
                var routeValues = context.GetRouteData().Values;
                var controllerName = routeValues.GetControllerName().ToLowerInvariant();
                var actionName = routeValues.GetActionName().ToLowerInvariant();

                string currentPageType = "system";
                object currentPageId = controllerName + "." + actionName;

                if (displayHelper.IsHomePage())
                {
                    currentPageType = "home";
                    currentPageId = 0;
                }
                else if (controllerName == "catalog")
                {
                    if (actionName == "category")
                    {
                        currentPageType = "category";
                        currentPageId = routeValues.Get("categoryId");
                    }
                    else if (actionName == "manufacturer")
                    {
                        currentPageType = "brand";
                        currentPageId = routeValues.Get("manufacturerId");
                    }
                }
                else if (controllerName == "product")
                {
                    if (actionName == "productdetails")
                    {
                        currentPageType = "product";
                        currentPageId = routeValues.Get("productId");
                    }
                }
                else if (controllerName == "topic")
                {
                    if (actionName == "topicdetails")
                    {
                        currentPageType = "topic";
                        currentPageId = routeValues.Get("topicId");
                    }
                }

                return new PageIdentity { CurrentPageId = currentPageId, CurrentPageType = currentPageType };
            });
        }

        class PageIdentity
        {
            public string CurrentPageType { get; set; }
            public object CurrentPageId { get; set; }
        }
    }
}
