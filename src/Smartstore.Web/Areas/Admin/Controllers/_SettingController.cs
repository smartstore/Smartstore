using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Admin.Models;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Controllers
{
    public class SettingController : AdminControllerBase
    {
        private readonly SmartDbContext _db;
        
        public SettingController(SmartDbContext db)
        {
            _db = db;
        }

        //private StoreDependingSettingHelper StoreDependingSettings
        //{
        //    get
        //    {
        //        if (_storeDependingSettings == null)
        //        {
        //            _storeDependingSettings = new StoreDependingSettingHelper(ViewData);
        //        }

        //        return _storeDependingSettings;
        //    }
        //}

        [LoadSetting(IsRootedModel = true)]
        public ActionResult GeneralCommon(int storeScope,
            StoreInformationSettings storeInformationSettings,
            SeoSettings seoSettings,
            DateTimeSettings dateTimeSettings,
            SecuritySettings securitySettings,
            CaptchaSettings captchaSettings,
            PdfSettings pdfSettings,
            LocalizationSettings localizationSettings,
            CompanyInformationSettings companySettings,
            ContactDataSettings contactDataSettings,
            BankConnectionSettings bankConnectionSettings,
            SocialSettings socialSettings,
            HomePageSettings homePageSettings)
        {
            // TODO: (mh) (core) 
            // Set page timeout to 5 minutes.
            //Server.ScriptTimeout = 300;

            var model = new GeneralCommonSettingsModel();

            return View(model);
        }

        public async Task<IActionResult> ChangeStoreScopeConfiguration(int storeid, string returnUrl = "")
        {
            var store = Services.StoreContext.GetStoreById(storeid);
            if (store != null || storeid == 0)
            {
                Services.WorkContext.CurrentCustomer.GenericAttributes.AdminAreaStoreScopeConfiguration = storeid;
                await _db.SaveChangesAsync();
            }

            return RedirectToReferrer(returnUrl, () => RedirectToAction("Index", "Home", new { area = "Admin" }));
        }
    }
}