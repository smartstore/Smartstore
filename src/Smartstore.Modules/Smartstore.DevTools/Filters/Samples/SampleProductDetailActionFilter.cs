using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core;
using Smartstore.Core.Web;
using Smartstore.Web.Models.Catalog;

namespace Smartstore.DevTools.Filters.Samples
{
    internal class SampleProductDetailActionFilter : IActionFilter
    {
        private readonly ICommonServices _services;
        private readonly IViewDataAccessor _viewDataAccessor;
        private readonly IUrlHelper _urlHelper;

        public SampleProductDetailActionFilter(ICommonServices services, IViewDataAccessor viewDataAccessor, IUrlHelper urlHelper)
        {
            _services = services;
            _viewDataAccessor = viewDataAccessor;
            _urlHelper = urlHelper;
        }

        public void OnActionExecuting(ActionExecutingContext filterContext)
        {

        }

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (!filterContext.Result.IsHtmlViewResult())
            {
                // The controller action didn't return a view result 
                // => no need to continue any further
                return;
            }

            var viewData = _viewDataAccessor.ViewData;
            if (viewData == null || viewData.Model is not ProductDetailsModel model)
            {
                // There is no model or the model was not of the expected type 
                // => no need to continue any further
                return;
            }

            // modify some property value
            model.ActionItems["dev"] = new ProductDetailsModel.ActionItemModel
            {
                Key = "dev",
                Title = _services.Localization.GetResource("Dev"),
                Tooltip = _services.Localization.GetResource("Dev.Hint"),
                CssClass = "action-dev ajax-cart-link",
                IconCssClass = "icm icm-server",
                //Href = _urlHelper.Action("MyOwnAction", "MyPlugin", new { id = model.Id })
                Href = "https://www.smartstore.com"
            };
        }
    }
}
