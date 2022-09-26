using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Smartstore.Web.Api
{
    /// <summary>
    /// Convention to omit controllers and actions from API explorer that are not decorated with <see cref="ApiExplorerSettingsAttribute"/>.
    /// </summary>
    public class ApiControllerModelConvention : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            controller.ApiExplorer.IsVisible = controller.ControllerType.HasAttribute<ApiExplorerSettingsAttribute>(true);
        }
    }

    // TODO: (mg) (core) cleanup old API stuff later.

    //[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    //public class ApiDocumentAttribute : Attribute
    //{
    //    /// <summary>
    //    /// Sets the group name based on the API controller's namespace. Examples: 
    //    /// Smartstore.Web.Api.Controllers.V1.CategoriesController -> group name "v1"
    //    /// Smartstore.Web.Api.Controllers.MyApi.V2.AnyController  -> group name "myapi.v2"
    //    /// </summary>
    //    /// <remarks>Same result as if you would set the group name by ApiExplorerSettingsAttribute.</remarks>
    //    public bool SetGroupNameByNamespace { get; set; } = true;
    //}

    //public class ApiExplorerConvention1 : IActionModelConvention
    //{
    //    public void Apply(ActionModel action)
    //    {
    //        var attribute = action.Controller.ControllerType.GetAttribute<ApiDocumentAttribute>(true);

    //        if (attribute == null && !action.ActionMethod.HasAttribute<ApiDocumentAttribute>(true))
    //        {
    //            action.ApiExplorer.IsVisible = false;
    //        }
    //        else if (attribute != null && attribute.SetGroupNameByNamespace)
    //        {
    //            var idx = action.Controller.ControllerType.Namespace.IndexOf(".Controllers.");
    //            if (idx == -1)
    //            {
    //                throw new InvalidOperationException($"Cannot set API explorer group name from namespace '{action.Controller.ControllerType.Namespace}'.");
    //            }

    //            action.ApiExplorer.GroupName = action.Controller.ControllerType.Namespace[(idx + 13)..].ToLower();

    //            //$"{action.ApiExplorer.GroupName.NaIfEmpty()} {action.DisplayName}".Dump();
    //        }
    //    }
    //}
}
