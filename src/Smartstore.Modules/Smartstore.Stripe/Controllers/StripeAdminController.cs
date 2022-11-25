using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.ComponentModel;
using Smartstore.Core.Data;
using Smartstore.Core.Security;
using Smartstore.Engine.Modularity;
using Smartstore.StripeElements.Models;
using Smartstore.StripeElements.Providers;
using Smartstore.StripeElements.Settings;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;

namespace Smartstore.StripeElements.Controllers
{
    [Area("Admin")]
    public class StripeAdminController : ModuleController
    {
        private readonly SmartDbContext _db;
        private readonly IProviderManager _providerManager;

        public StripeAdminController(SmartDbContext db, IProviderManager providerManager)
        {
            _db = db;
            _providerManager = providerManager;
        }

        [LoadSetting, AuthorizeAdmin]
        public IActionResult Configure(StripeSettings settings)
        {
            ViewBag.Provider = _providerManager.GetProvider(StripeElementsProvider.SystemName).Metadata;

            var model = MiniMapper.Map<StripeSettings, ConfigurationModel>(settings);

            ViewBag.AvailableCaptureMethods = new List<SelectListItem>
            {
                new SelectListItem()
                {
                    Text = T("Plugins.Smartstore.Stripe.CaptureMethod.Automatic"),
                    Value = "automatic",
                    Selected = "automatic" == settings.CaptureMethod
                },
                new SelectListItem()
                {
                    Text = T("Plugins.Smartstore.Stripe.CaptureMethod.Manual"),
                    Value = "manual",
                    Selected = "manual" == settings.CaptureMethod
                }
            };

            return View(model);
        }

        [HttpPost, SaveSetting, AuthorizeAdmin]
        public IActionResult Configure(ConfigurationModel model, StripeSettings settings)
        {
            if (!ModelState.IsValid)
            {
                return Configure(settings);
            }

            ModelState.Clear();
            MiniMapper.Map(model, settings);

            return RedirectToAction(nameof(Configure));
        }
    }
}