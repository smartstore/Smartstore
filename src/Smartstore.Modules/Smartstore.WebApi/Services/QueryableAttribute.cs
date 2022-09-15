using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.DependencyInjection;

namespace Smartstore.Web.Api.Services
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class QueryableAttribute : EnableQueryAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext actionExecutedContext)
        {
            SetDefaultQueryOptions(actionExecutedContext);
            base.OnActionExecuted(actionExecutedContext);
        }

        protected virtual void SetDefaultQueryOptions(ActionExecutedContext actionExecutedContext)
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
