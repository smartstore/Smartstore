using Microsoft.AspNetCore.Mvc;
using Smartstore.Admin.Models.Modularity;
using Smartstore.Core.Security;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.Web.Controllers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Smartstore.Admin.Controllers
{
    public partial class WidgetController : AdminController
    {
        private readonly IWidgetService _widgetService;
        private readonly IProviderManager _providerManager;
        private readonly WidgetSettings _widgetSettings;
        private readonly ModuleManager _moduleManager;

        public WidgetController(
            IWidgetService widgetService,
            IProviderManager providerManager,
            WidgetSettings widgetSettings,
            ModuleManager moduleManager)
        {
            _widgetService = widgetService;
            _providerManager = providerManager;
            _widgetSettings = widgetSettings;
            _moduleManager = moduleManager;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Providers");
        }

        [Permission(Permissions.Cms.Widget.Read)]
        public IActionResult Providers()
        {
            var widgetsModel = new List<WidgetModel>();
            var widgets = _providerManager.GetAllProviders<IWidget>();

            foreach (var widget in widgets)
            {
                var model = _moduleManager.ToProviderModel<IWidget, WidgetModel>(widget);
                model.IsActive = widget.IsWidgetActive(_widgetSettings);
                widgetsModel.Add(model);
            }

            return View(widgetsModel);
        }

        [HttpPost]
        [Permission(Permissions.Cms.Widget.Activate)]
        public async Task<IActionResult> ActivateProvider(string systemName, bool activate)
        {
            await _widgetService.ActivateWidgetAsync(systemName, activate);
            return RedirectToAction("Providers");
        }
    }
}
