using Microsoft.AspNetCore.Mvc.Filters;

namespace Smartstore.Web.Filters
{
    internal class ConditionalFilterProvider : IFilterProvider
    {
        public int Order => -20000;

        public void OnProvidersExecuting(FilterProviderContext context)
        {
            context.Results.Remove(x => IsNonMatchingFilter(context.ActionContext, x));
        }

        private static bool IsNonMatchingFilter(ActionContext context, FilterItem item)
        {
            if (item.Descriptor.Filter is IFilterConstraint constraint)
            {
                if (constraint.Match(context))
                {
                    if (item.Filter == null)
                    {
                        item.IsReusable = false;
                        item.Filter = constraint.CreateInstance(context.HttpContext.RequestServices);
                    }
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        public void OnProvidersExecuted(FilterProviderContext context)
        {
        }
    }
}
