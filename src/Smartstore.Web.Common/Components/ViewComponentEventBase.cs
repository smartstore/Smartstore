using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Smartstore.Web.Components
{
    public abstract class ViewComponentEventBase
    {
        protected ViewComponentEventBase(ViewComponentContext context)
        {
            Guard.NotNull(context, nameof(context));

            ViewComponentContext = context;
        }

        public ViewComponentContext ViewComponentContext { get; }

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

        public Type ComponentType
        {
            get => Descriptor.TypeInfo.AsType();
        }
    }
}