using Smartstore.Admin.Models;
using Smartstore.Core.Catalog.Search.Modelling;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Web.Modelling.Settings;

namespace Smartstore.Admin.Controllers
{
    public partial class SettingController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ILanguageService _languageService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly MultiStoreSettingHelper _multiStoreSettingHelper;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly Lazy<IMediaTracker> _mediaTracker;
        private readonly Lazy<ICatalogSearchQueryAliasMapper> _catalogSearchQueryAliasMapper;

        public SettingController(
            SmartDbContext db,
            ILanguageService languageService,
            ILocalizedEntityService localizedEntityService,
            MultiStoreSettingHelper multiStoreSettingHelper,
            IDateTimeHelper dateTimeHelper,
            Lazy<IMediaTracker> mediaTracker,
            Lazy<ICatalogSearchQueryAliasMapper> catalogSearchQueryAliasMapper)
        {
            _db = db;
            _languageService = languageService;
            _localizedEntityService = localizedEntityService;
            _multiStoreSettingHelper = multiStoreSettingHelper;
            _dateTimeHelper = dateTimeHelper;
            _mediaTracker = mediaTracker;
            _catalogSearchQueryAliasMapper = catalogSearchQueryAliasMapper;
        }

        [Permission(Permissions.Configuration.Setting.Read)]
        [HttpPost, LoadSetting]
        public IActionResult TestSeoNameCreation(SeoSettings settings, GeneralCommonSettingsModel model)
        {
            // We always test against persisted settings.
            var result = SlugUtility.Slugify(
                model.SeoSettings.TestSeoNameCreation,
                settings.ConvertNonWesternChars,
                settings.AllowUnicodeCharsInUrls,
                SeoSettings.CreateCharConversionMap(settings.SeoNameCharConversion));

            return Content(result);
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