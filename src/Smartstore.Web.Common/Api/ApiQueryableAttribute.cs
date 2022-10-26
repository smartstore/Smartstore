using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.OData.Query;

namespace Smartstore.Web.Api
{
    /// <inheritdoc/>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ApiQueryableAttribute : EnableQueryAttribute
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
                var httpContext = actionExecutedContext.HttpContext;

                if (MaxTop != int.MaxValue)
                {
                    if (httpContext.Items.TryGetValue("Smartstore.WebApi.MaxTop", out var rawMaxTop) && rawMaxTop != null)
                    {
                        MaxTop = (int)rawMaxTop;
                    }

                    var hasClientPaging = httpContext?.Request?.Query?.Any(x => x.Key == "$top") ?? false;
                    if (!hasClientPaging)
                    {
                        // If paging is required and there is no $top sent by client then force the page size specified by merchant.
                        PageSize = MaxTop;
                    }
                }

                if (MaxExpansionDepth != int.MaxValue
                    && httpContext.Items.TryGetValue("Smartstore.WebApi.MaxExpansionDepth", out var rawMaxExpansionDepth) 
                    && rawMaxExpansionDepth != null)
                {
                    MaxExpansionDepth = (int)rawMaxExpansionDepth;
                }

                //$"ApiQueryable MaxTop:{MaxTop} MaxExpansionDepth:{MaxExpansionDepth}".Dump();
            }
            catch
            {
            }
        }
    }
}
