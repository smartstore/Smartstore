using Microsoft.AspNetCore.Mvc.Filters;

namespace Smartstore.Web.Modelling
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

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            context.ActionArguments[_actionParameterName] = context.HttpContext.Request.Form.ContainsKey(_name);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}
