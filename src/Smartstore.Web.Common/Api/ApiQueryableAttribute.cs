using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.OData.Query;

namespace Smartstore.Web.Api
{
    public class MaxApiQueryOptions
    {
        public static readonly string Key = "Smartstore.WebApi.MaxQueryOptions";

        public int MaxTop { get; init; }
        public int MaxExpansionDepth { get; init; }
    }

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
                var context = actionExecutedContext.HttpContext;

                if (context.Items.TryGetValue(MaxApiQueryOptions.Key, out var obj) && obj is MaxApiQueryOptions maxValues)
                {
                    if (MaxTop != int.MaxValue)
                    {
                        MaxTop = maxValues.MaxTop;

                        var hasClientPaging = context?.Request?.Query?.Any(x => x.Key == "$top") ?? false;
                        if (!hasClientPaging)
                        {
                            // If paging is required and there is no $top sent by client then force the page size specified by merchant.
                            PageSize = MaxTop;
                        }
                    }

                    if (MaxExpansionDepth != int.MaxValue)
                    {
                        MaxExpansionDepth = maxValues.MaxExpansionDepth;
                    }
                }

                //$"ApiQueryable MaxTop:{MaxTop} MaxExpansionDepth:{MaxExpansionDepth}".Dump();
            }
            catch
            {
            }
        }
    }
}
