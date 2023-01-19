using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NuGet.Configuration;
using Smartstore.ComponentModel;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.Paystack.Configuration;
using Smartstore.Paystack.Models;
using Smartstore.Paystack.Providers;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;

namespace Smartstore.Paystack.Controllers
{
    [Area("Admin")]
    [Route("[area]/paystack/{action=index}/{id?}")]
    public class PaystackAdminController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IProviderManager _providerManager;
        private readonly MultiStoreSettingHelper _settingHelper;
        private readonly CompanyInformationSettings _companyInformationSettings;

        public PaystackAdminController(
             SmartDbContext db,
            IProviderManager providerManager,
            MultiStoreSettingHelper settingHelper,
            CompanyInformationSettings companyInformationSettings)
        {
            _db = db;
            _providerManager = providerManager;
            _settingHelper = settingHelper;
            _companyInformationSettings = companyInformationSettings;
        }

        [LoadSetting]
        public async Task<IActionResult> Configure(PaystackSettings settings)
        {
            ViewBag.Provider = _providerManager.GetProvider(PaystackProvider.SystemName).Metadata;

            var language = Services.WorkContext.WorkingLanguage;
            var store = Services.StoreContext.CurrentStore;
            var module = Services.ApplicationContext.ModuleCatalog.GetModuleByName("Smartstore.Paystack");
            var currentScheme = Services.WebHelper.IsCurrentConnectionSecured() ? "https" : "http";
          //  var topicUrl = await Url.TopicAsync("privacyinfo");

            var model = MiniMapper.Map<PaystackSettings, ConfigurationModel>(settings);

            return View(model);

        }

        [HttpPost]
        public async Task<IActionResult> Configure(ConfigurationModel model, IFormCollection form)
        {
            var storeScope = GetActiveStoreScopeConfiguration();
            var settings = await Services.SettingFactory.LoadSettingsAsync<PaystackSettings>(storeScope);

            if (!ModelState.IsValid)
            {
                return await Configure(settings);
            }

            ModelState.Clear();

            model.PublicKey = model.PublicKey.TrimSafe();
            model.PrivateKey = model.PrivateKey.TrimSafe();
            model.BaseUrl = model.BaseUrl.TrimSafe();

           // settings = ((ISettings)settings).Clone() as PaystackSettings;
            MiniMapper.Map(model, settings);

            _settingHelper.Contextualize(storeScope);
            await _settingHelper.UpdateSettingsAsync(settings, form);
            await _db.SaveChangesAsync();

            NotifySuccess(T("Admin.Common.DataSuccessfullySaved"));

            return RedirectToAction(nameof(Configure));
        }

    }
}
