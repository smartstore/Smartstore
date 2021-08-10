using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Engine.Modularity;
using Smartstore.Web.Controllers;

namespace Smartstore.Admin.Controllers
{
    public class ModuleController : AdminController
    {
        private readonly IModuleCatalog _moduleCatalog;
        private readonly ILanguageService _languageService;
        private readonly IXmlResourceManager _xmlResourceManager;
        private readonly IProviderManager _providerManager;
        //private readonly PluginMediator _pluginMediator;

        public ModuleController(
            IModuleCatalog moduleCatalog,
            ILanguageService languageService,
            IXmlResourceManager xmlResourceManager,
            IProviderManager providerManager)
        {
            _moduleCatalog = moduleCatalog;
            _languageService = languageService;
            _xmlResourceManager = xmlResourceManager;
            _providerManager = providerManager;
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Module.Update)]
        public async Task<IActionResult> UpdateStringResources(string systemName)
        {
            var moduleDescriptor = _moduleCatalog.GetModuleByName(systemName);

            var success = false;
            var message = "";
            if (moduleDescriptor == null)
            {
                message = T("Admin.Configuration.Plugins.Resources.UpdateFailure").Value;
            }
            else
            {
                await _xmlResourceManager.ImportModuleResourcesFromXmlAsync(moduleDescriptor, null, false);

                success = true;
                message = T("Admin.Configuration.Plugins.Resources.UpdateSuccess").Value;
            }

            return Json(new
            {
                Success = success,
                Message = message
            });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Module.Update)]
        public async Task<IActionResult> UpdateAllStringResources()
        {
            var moduleDescriptors = _moduleCatalog.Modules;

            foreach (var descriptor in moduleDescriptors)
            {
                if (descriptor.Module?.Installed == true)
                {
                    await _xmlResourceManager.ImportModuleResourcesFromXmlAsync(descriptor, null, false);
                }
                else
                {
                    await Services.Localization.DeleteLocaleStringResourcesAsync(descriptor.ResourceRootKey);
                }
            }

            return Json(new
            {
                Success = true,
                Message = T("Admin.Configuration.Plugins.Resources.UpdateSuccess").Value
            });
        }
    }
}
