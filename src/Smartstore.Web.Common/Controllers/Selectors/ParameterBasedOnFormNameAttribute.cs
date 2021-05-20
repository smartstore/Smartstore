using Microsoft.AspNetCore.Mvc.Filters;

namespace Smartstore.Web.Controllers
{
    /// <summary>
    /// If form name exists, then specified "actionParameterName" will be set to "true".
    /// </summary>
    public class ParameterBasedOnFormNameAttribute : ActionFilterAttribute
    {
        private readonly string _name;
        private readonly string _actionParameterName;

        public ParameterBasedOnFormNameAttribute(string name, string actionParameterName)
        {
            _name = name;
            _actionParameterName = actionParameterName;
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var formValue = filterContext.HttpContext.Request.Form[_name];
            filterContext.ActionArguments[_actionParameterName] = !string.IsNullOrEmpty(formValue);
        }
    }
}
