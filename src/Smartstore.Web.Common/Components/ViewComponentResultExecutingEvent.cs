using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Smartstore.Web.Components
{
    public class ViewComponentResultExecutingEvent
    {
        public ViewComponentResultExecutingEvent(ViewComponentContext context, ViewViewComponentResult result)
        {
            Guard.NotNull(context, nameof(context));
            Guard.NotNull(result, nameof(result));

            ViewComponentContext = context;
        }

        public ViewComponentContext ViewComponentContext { get; }
        public ViewViewComponentResult Result { get; set; }

        public string ViewName 
        {
            get => Result.ViewName;
            set => Result.ViewName = value;
        }
        public object Model
        {
            get => Result.ViewData.Model;
            set => Result.ViewData.Model = value;
        }

        public HttpContext HttpContext 
        { 
            get => ViewComponentContext.ViewContext.HttpContext;
        }

        public ViewDataDictionary ViewData
        {
            get => ViewComponentContext.ViewData;
        }

        public ViewComponentDescriptor Descriptor
        {
            get => ViewComponentContext.ViewComponentDescriptor;
        }
    }
}