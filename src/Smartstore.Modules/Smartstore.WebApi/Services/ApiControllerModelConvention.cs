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
}
