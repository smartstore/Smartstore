using Microsoft.AspNetCore.Mvc.Filters;

namespace Smartstore.Web.Filters
{
    public interface IFilterConstraint : IFilterFactory
    {
        bool Match(ActionContext context);
    }
}
