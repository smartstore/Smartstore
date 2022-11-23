#nullable enable

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Smartstore.Core.Web
{
    public class DefaultViewDataAccessor : IViewDataAccessor, IActionFilter
    {
        internal const string ViewDataAccessKey = "__CurrentViewDataDictionary";

        private readonly IHttpContextAccessor _httpContextAccessor;

        public DefaultViewDataAccessor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public ViewDataDictionary? ViewData 
            => _httpContextAccessor.HttpContext?.Items[ViewDataAccessKey] as ViewDataDictionary;

        void IActionFilter.OnActionExecuting(ActionExecutingContext context)
        {
            if (context.Controller is Controller controller)
            {
                controller.HttpContext.Items[ViewDataAccessKey] = controller.ViewData;             
            }
        }

        void IActionFilter.OnActionExecuted(ActionExecutedContext context)
        {
            // Noop
        }
    }
}
