using Microsoft.AspNetCore.Mvc.Razor;
using Smartstore.Web.Controllers;
using Smartstore.Web.Theming;

namespace Smartstore.Web.Razor
{
    /// <summary>
    /// In NonAdmin areas, but "admin-themed" endpoints, adds "/Areas/Admin/Views/{1}/{0}.cshtml" and "/Areas/Admin/Views/Shared/{1}/{0}.cshtml" as fallback view locations.
    /// </summary>
    internal class AdminViewLocationExpander : IViewLocationExpander
    {
        const string ParamKey = "admin-themed";

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            if (context.Values.ContainsKey(ParamKey))
            {
                return viewLocations.Union(new[]
                {
                    "/Areas/Admin/Views/{1}/{0}" + RazorViewEngine.ViewExtension,
                    "/Areas/Admin/Views/Shared/{0}" + RazorViewEngine.ViewExtension
                });
            }

            return viewLocations;
        }

        public void PopulateValues(ViewLocationExpanderContext context)
        {
            if (context.ActionContext.HttpContext.Response.StatusCode < 300
                && context.AreaName.HasValue()
                && !context.AreaName.EqualsNoCase("admin"))
            {
                var metadata = context.ActionContext.ActionDescriptor.EndpointMetadata;
                if (metadata.OfType<AdminThemedAttribute>().Any() && !metadata.OfType<NonAdminAttribute>().Any())
                {
                    context.Values[ParamKey] = "true";
                }
            }
        }
    }
}
