//using System;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Html;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.AspNetCore.Mvc.ViewFeatures;
//using Microsoft.Extensions.DependencyInjection;

//namespace Smartstore.Core.Widgets
//{
//    public class PartialViewWidgetInvoker : WidgetInvoker
//    {
//        private readonly string _partialName;
//        private readonly string _module;

//        public PartialViewWidgetInvoker(string partialName, string module = null)
//        {
//            Guard.NotEmpty(partialName, nameof(partialName));

//            _partialName = partialName;
//            _module = module;
//        }

//        public override Task<IHtmlContent> InvokeAsync(ViewContext viewContext)
//        {
//            return InvokeAsync(viewContext, null);
//        }

//        public Task<IHtmlContent> InvokeAsync(ViewContext viewContext, object model)
//        {
//            var helper = viewContext.HttpContext.RequestServices.GetService<IHtmlHelper>();

//            if (_module.HasValue())
//            {
//                viewContext = ModifyViewContext(viewContext);
//            }

//            return helper.PartialAsync(_partialName, model ?? helper.ViewData.Model);
//        }

//        private static ViewContext ModifyViewContext(ViewContext viewContext)
//        {
//            return viewContext;
//        }
//    }
//}
