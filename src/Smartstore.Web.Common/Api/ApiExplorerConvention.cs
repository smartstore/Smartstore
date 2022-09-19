using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Smartstore.Web.Api
{
    /// <summary>
    /// Specifies a class or method to be included in API explorer and thus Swagger documentation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ApiDocumentAttribute : Attribute
    {
        /// <summary>
        /// Sets the group name based on the API controller's namespace. Examples: 
        /// Smartstore.Web.Api.Controllers.V1.CategoriesController -> group name "v1"
        /// Smartstore.Web.Api.Controllers.MyApi.V2.AnyController  -> group name "myapi.v2"
        /// </summary>
        /// <remarks>Same result as if you would set the group name by ApiExplorerSettingsAttribute.</remarks>
        public bool SetGroupNameByNamespace { get; set; } = true;
    }

    /// <summary>
    /// Convention to omit controllers and actions that are not decorated with <see cref="ApiDocumentAttribute"/>.
    /// </summary>
    public class ApiExplorerConvention : IActionModelConvention
    {
        public void Apply(ActionModel action)
        {
            var attribute = action.Controller.ControllerType.GetAttribute<ApiDocumentAttribute>(true);

            if (attribute == null && !action.ActionMethod.HasAttribute<ApiDocumentAttribute>(true))
            {
                action.ApiExplorer.IsVisible = false;
            }
            else if (attribute != null && attribute.SetGroupNameByNamespace)
            {
                var idx = action.Controller.ControllerType.Namespace.IndexOf(".Controllers.");
                if (idx == -1)
                {
                    throw new InvalidOperationException($"Cannot set API explorer group name from namespace '{action.Controller.ControllerType.Namespace}'.");
                }

                action.ApiExplorer.GroupName = action.Controller.ControllerType.Namespace[(idx + 13)..].ToLower();
            }

            //$"{action.ApiExplorer.GroupName.NaIfEmpty()} {action.DisplayName}".Dump();
        }
    }
}
