using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace Smartstore.Web.UI
{
    public class ViewComponentWidgetInvoker : WidgetInvoker
    {
        private readonly string _name;
        private readonly object _arguments;

        public ViewComponentWidgetInvoker(string name, object arguments)
        {
            Guard.NotEmpty(name, nameof(name));

            _name = name;
            _arguments = arguments;
        }

        public override Task<IHtmlContent> InvokeAsync(ViewContext viewContext)
        {
            var helper = CreateViewComponentHelper(viewContext);
            return helper.InvokeAsync(_name, _arguments);
        }

        private static IViewComponentHelper CreateViewComponentHelper(ViewContext viewContext)
        {
            var viewComponentHelper = viewContext.HttpContext.RequestServices.GetService<IViewComponentHelper>();
            var hasViewContext = viewComponentHelper as IViewContextAware;
            if (viewComponentHelper is IViewContextAware viewContextAware)
            {
                viewContextAware.Contextualize(viewContext);
            }

            return viewComponentHelper;
        }
    }
}
