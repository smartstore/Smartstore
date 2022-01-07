using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace Smartstore.Web.Routing
{
    /// <summary>
    /// An implementation of <see cref="IUrlHelper"/> that uses <see cref="LinkGenerator"/> to build URLs
    /// for ASP.NET MVC within an application.
    /// </summary>
    /// <remarks>
    /// This class is a copy of the original EndpointRoutingUrlHelper with slight modifications.
    /// MVC does not create EndpointRoutingUrlHelper if Endpoint is null (creates legacy UrlHelper that uses IRouter instead in this case).
    /// But calling Action() or RouteUrl() does not seem to fail here even without an endpoint.
    /// And we need URL resolution in middlewares.
    /// </remarks>
    public class SmartUrlHelper : UrlHelperBase
    {
        private readonly LinkGenerator _linkGenerator;

        public SmartUrlHelper(ActionContext actionContext, LinkGenerator linkGenerator)
            : base(actionContext)
        {
            _linkGenerator = Guard.NotNull(linkGenerator, nameof(linkGenerator));
        }

        /// <inheritdoc />
        public override string Action(UrlActionContext urlActionContext)
        {
            Guard.NotNull(urlActionContext, nameof(urlActionContext));

            var values = GetValuesDictionary(urlActionContext.Values);

            if (urlActionContext.Action == null)
            {
                if (!values.ContainsKey("action") && AmbientValues.TryGetValue("action", out var action))
                {
                    values["action"] = action;
                }
            }
            else
            {
                values["action"] = urlActionContext.Action;
            }

            if (urlActionContext.Controller == null)
            {
                if (!values.ContainsKey("controller") && AmbientValues.TryGetValue("controller", out var controller))
                {
                    values["controller"] = controller;
                }
            }
            else
            {
                values["controller"] = urlActionContext.Controller;
            }


            var path = _linkGenerator.GetPathByRouteValues(
                ActionContext.HttpContext,
                routeName: null,
                values,
                fragment: urlActionContext.Fragment == null ? FragmentString.Empty : new FragmentString("#" + urlActionContext.Fragment));

            return GenerateUrl(urlActionContext.Protocol, urlActionContext.Host, path);
        }

        /// <inheritdoc />
        public override string RouteUrl(UrlRouteContext routeContext)
        {
            Guard.NotNull(routeContext, nameof(routeContext));

            var path = _linkGenerator.GetPathByRouteValues(
                ActionContext.HttpContext,
                routeContext.RouteName,
                routeContext.Values,
                fragment: routeContext.Fragment == null ? FragmentString.Empty : new FragmentString("#" + routeContext.Fragment));

            return GenerateUrl(routeContext.Protocol, routeContext.Host, path);
        }
    }
}
