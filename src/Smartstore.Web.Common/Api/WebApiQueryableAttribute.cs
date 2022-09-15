using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.OData.Query;

namespace Smartstore.Web.Api
{
    /// <inheritdoc/>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class WebApiQueryableAttribute : EnableQueryAttribute
    {
        /// <inheritdoc/>
        public override void OnActionExecuted(ActionExecutedContext actionExecutedContext)
        {
            ApplyDefaultQueryOptions(actionExecutedContext);
            base.OnActionExecuted(actionExecutedContext);
        }

        protected virtual void ApplyDefaultQueryOptions(ActionExecutedContext actionExecutedContext)
        {
            try
            {
                if (MaxTop == 0)
                {
                    var apiService = actionExecutedContext.HttpContext.RequestServices.GetRequiredService<IWebApiService>();
                    var state = apiService.GetState();

                    MaxTop = state.MaxTop;
                    MaxExpansionDepth = state.MaxExpansionDepth;
                }

                var hasClientPaging = actionExecutedContext?.HttpContext?.Request?.Query?.Any(x => x.Key == "$top") ?? false;
                if (!hasClientPaging)
                {
                    // If paging is required and there is no $top sent by client then force the page size specified by merchant.
                    PageSize = MaxTop;
                }
            }
            catch
            {
            }
        }
    }
}
