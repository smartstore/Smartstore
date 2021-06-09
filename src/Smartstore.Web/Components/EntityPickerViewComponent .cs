using Microsoft.AspNetCore.Mvc;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.Components
{
    public class EntityPickerViewComponent : SmartViewComponent
    {
        private readonly IUrlHelper _urlHelper;

        public EntityPickerViewComponent(IUrlHelper urlHelper)
        {
            _urlHelper = urlHelper;
        }

        public IViewComponentResult Invoke(EntityPickerConfigurationModel model)
        {
            if (model == null)
            {
                return Empty();
            }

            model.DialogUrl = _urlHelper.Action("Picker", "Entity", new { area = "" });

            return View(model);
        }
    }
}