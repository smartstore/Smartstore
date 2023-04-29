using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Web;

namespace Smartstore.Core.Seo.Routing
{
    /// <summary>
    /// Disallows robots access to the route. Also used to dynamically populate the robots.txt file.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class DisallowRobotAttribute : Attribute, IAuthorizationFilter, IOrderedFilter
    {
        public DisallowRobotAttribute()
        {
        }

        public DisallowRobotAttribute(bool exactPath)
        {
            ExactPath = exactPath;
        }

        public bool ExactPath { get; set; }

        /// <inheritdoc />
        /// <value>Default is <c>int.MinValue + 50</c> to run this <see cref="IAuthorizationFilter"/> early.</value>
        public int Order { get; set; } = int.MinValue + 50;

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var userAgent = context.HttpContext.RequestServices.GetRequiredService<IUserAgent>();
            if (userAgent.IsBot())
            {
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
            }
        }
    }
}