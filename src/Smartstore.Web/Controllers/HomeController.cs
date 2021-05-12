using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using Smartstore.Events;
using Smartstore.Engine;
using Smartstore.Web.Models;
using Smartstore.Core.Configuration;
using Smartstore.Core.Stores;
using Smartstore.Caching;
using Smartstore.Threading;
using System.Threading;
using Smartstore.Web.Theming;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core;
using Smartstore.Core.Logging;
using LogLevel = Smartstore.Core.Logging.LogLevel;
using System.Text;
using Smartstore.Core.Common;
using Smartstore.Data.Caching;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Microsoft.AspNetCore.Routing;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Imaging;
using Smartstore.Core.Content.Media.Storage;
using System.IO;
using System.Drawing;
using Humanizer;
using System.Numerics;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.GiftCards;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Security;
using Smartstore.Collections;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Attributes;
using Microsoft.AspNetCore.Identity;
using Smartstore.Core.Identity;
using Microsoft.Extensions.Options;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Web;
using Microsoft.AspNetCore.Authorization;
using StackExchange.Profiling.Internal;
using Smartstore.Core.Messaging;
using Smartstore.Core.Rules.Rendering;
using Smartstore.Core.Rules;
using SixLabors.ImageSharp.ColorSpaces;
using Smartstore.Core.Identity.Rules;
using Smartstore.Core.Rules.Filters;
using Microsoft.Extensions.Configuration;
using Smartstore.Web.Filters;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Domain;
using Smartstore.Scheduling;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Data;
using Smartstore.Core.Common.Settings;
using System.Runtime.InteropServices;
using Smartstore.Core.Catalog;
using Smartstore.Utilities;
using Smartstore.ComponentModel;
using Smartstore.Web.Models.Common;
using Smartstore.Core.Content.Topics;
using Smartstore.Utilities.Html;
using Smartstore.Net.Mail;
using Autofac.Core;
using Smartstore.Core.Catalog.Categories;

namespace Smartstore.Web.Controllers
{
    // TODO: (mh) (core) Remove this.
    public class MyProgress
    {
        public int Percent { get; set; }
        public string Message { get; set; }
    }

    public class TestSettings : ISettings
    {
        public string Prop1 { get; set; } = "Prop1";
        public string Prop2 { get; set; } = "Prop2";
        public string Prop3 { get; set; } = "Prop3";
    }

    public class HomeController : PublicControllerBase
    {
        // TODO: (mh) (core) Clean this.
        private static CancellationTokenSource _cancelTokenSource = new();

        private readonly SmartDbContext _db;
        private readonly IEventPublisher _eventPublisher;
        private readonly IStoreContext _storeContext;
        private readonly ILogger<HomeController> _logger1;
        private readonly ILogger _logger2;
        private readonly ICacheManager _cache;
        private readonly IAsyncState _asyncState;
        private readonly IThemeRegistry _themeRegistry;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILocalizationService _locService;
        private readonly IImageFactory _imageFactory;
        private readonly IMediaStorageProvider _mediaStorageProvider;
        private readonly ISettingFactory _settingFactory;
        private readonly IShippingService _shippingService;
        private readonly IShoppingCartService _cartService;
        private readonly ICheckoutAttributeFormatter _checkoutAttributeFormatter;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly IGiftCardService _giftCardService;
        private readonly UserManager<Customer> _userManager;
        private readonly IUserAgent _userAgent;
        private readonly IMediaService _mediaService;
        private readonly HomePageSettings _homePageSettings;

        private readonly IMessageFactory _messageFactory;
        private readonly PrivacySettings _privacySettings;
        private readonly CaptchaSettings _captchaSettings;
        private readonly CommonSettings _commonSettings;

        public HomeController(
            SmartDbContext db,
            ILogger<HomeController> logger1,
            ILogger logger2,
            ISettingFactory settingFactory,
            IEventPublisher eventPublisher,
            IDbContextFactory<SmartDbContext> dbContextFactory,
            IStoreContext storeContext,
            IEnumerable<IDbSaveHook> hooks,
            ICacheManager cache,
            IAsyncState asyncState,
            IThemeRegistry themeRegistry,
            TaxSettings taxSettings,
            ILoggerFactory loggerFactory,
            ILocalizationService locService,
            IImageFactory imageFactory,
            IMediaStorageProvider mediaStorageProvider,
            IShippingService shippingService,
            IShoppingCartService cartService,
            ICheckoutAttributeFormatter checkoutAttributeFormatter,
            IOrderCalculationService orderCalculationService,
            IGiftCardService giftCardService,
            UserManager<Customer> userManager,
            IUserAgent userAgent,
            IMediaService mediaService,
            HomePageSettings homePageSettings,
            IMessageFactory messageFactory,
            PrivacySettings privacySettings,
            CaptchaSettings captchaSettings,
            CommonSettings commonSettings)
        {
            _db = db;
            _eventPublisher = eventPublisher;
            _settingFactory = settingFactory;
            _storeContext = storeContext;
            _logger1 = logger1;
            _logger2 = logger2;
            _cache = cache;
            _asyncState = asyncState;
            _themeRegistry = themeRegistry;
            _loggerFactory = loggerFactory;
            _locService = locService;
            _imageFactory = imageFactory;
            _mediaStorageProvider = mediaStorageProvider;
            _shippingService = shippingService;
            _cartService = cartService;
            _checkoutAttributeFormatter = checkoutAttributeFormatter;
            _orderCalculationService = orderCalculationService;
            _giftCardService = giftCardService;
            _userManager = userManager;
            _userAgent = userAgent;
            _mediaService = mediaService;
            _homePageSettings = homePageSettings;

            _messageFactory = messageFactory;
            _privacySettings = privacySettings;
            _captchaSettings = captchaSettings;
            _commonSettings = commonSettings;
        }

        [LocalizedRoute("/", Name = "Homepage")]
        public IActionResult Index()
        {
            var storeId = _storeContext.CurrentStore.Id;

            ViewBag.MetaTitle = _homePageSettings.GetLocalizedSetting(x => x.MetaTitle, storeId);
            ViewBag.MetaDescription = _homePageSettings.GetLocalizedSetting(x => x.MetaDescription, storeId);
            ViewBag.MetaKeywords = _homePageSettings.GetLocalizedSetting(x => x.MetaKeywords, storeId);

            return View();
        }

        [CheckStoreClosed(false)]
        [LocalizedRoute("/storeclosed", Name = "StoreClosed")]
        public IActionResult StoreClosed()
        {
            return View();
        }

        [GdprConsent]
        [LocalizedRoute("/contactus", Name = "ContactUs")]
        public async Task<IActionResult> ContactUs()
        {
            var topic = await _db.Topics.AsNoTracking()
                .Where(x => x.SystemName == "ContactUs")
                .ApplyStandardFilter()
                .FirstOrDefaultAsync();

            var model = new ContactUsModel
            {
                Email = Services.WorkContext.CurrentCustomer.Email,
                FullName = Services.WorkContext.CurrentCustomer.GetFullName(),
                FullNameRequired = _privacySettings.FullNameOnContactUsRequired,
                DisplayCaptcha = _captchaSettings.CanDisplayCaptcha && _captchaSettings.ShowOnContactUsPage,
                MetaKeywords = topic?.GetLocalized(x => x.MetaKeywords),
                MetaDescription = topic?.GetLocalized(x => x.MetaDescription),
                MetaTitle = topic?.GetLocalized(x => x.MetaTitle),
            };

            return View(model);
        }

        [HttpPost, ActionName("ContactUs")]
        [ValidateCaptcha, ValidateHoneypot, GdprConsent]
        [LocalizedRoute("/contactus", Name = "ContactUs")]
        public async Task<IActionResult> ContactUsSend(ContactUsModel model, string captchaError)
        {
            if (_captchaSettings.ShowOnContactUsPage && captchaError.HasValue())
            {
                ModelState.AddModelError(string.Empty, captchaError);
            }

            if (ModelState.IsValid)
            {
                var customer = Services.WorkContext.CurrentCustomer;
                var email = model.Email.Trim();
                var fullName = model.FullName;
                var subject = T("ContactUs.EmailSubject", Services.StoreContext.CurrentStore.Name);
                var body = HtmlUtils.ConvertPlainTextToHtml(model.Enquiry.HtmlEncode());

                // Required for some SMTP servers.
                MailAddress sender = null;
                if (!_commonSettings.UseSystemEmailForContactUsForm)
                {
                    sender = new MailAddress(email, fullName);
                }

                var msg = await _messageFactory.SendContactUsMessageAsync(customer, email, fullName, subject, body, sender);

                if (msg?.Email?.Id != null)
                {
                    model.SuccessfullySent = true;
                    model.Result = T("ContactUs.YourEnquiryHasBeenSent");
                    Services.ActivityLogger.LogActivity(KnownActivityLogTypes.PublicStoreContactUs, T("ActivityLog.PublicStore.ContactUs"));
                }
                else
                {
                    ModelState.AddModelError(string.Empty, T("Common.Error.SendMail"));
                    model.Result = T("Common.Error.SendMail");
                }

                return View(model);
            }

            model.DisplayCaptcha = _captchaSettings.CanDisplayCaptcha && _captchaSettings.ShowOnContactUsPage;

            return View(model);
        }

        public IActionResult Sitemap()
        {
            return RedirectPermanent(Services.StoreContext.CurrentStore.Url);
        }

        #region Testing area

        public async Task<IActionResult> ClearCache()
        {
            // TODO: (core) Remove HomeController.ClearCache() later.
            await _cache.ClearAsync();
            return RedirectToReferrer();
        }

        //[LocalizedRoute("/", Name = "Homepage")]
        //public async Task<IActionResult> Index()
        //{

        //    #region Settings Test
        //    ////var xxx = await Services.Settings.GetSettingByKeyAsync<bool>("CatalogSettings.ShowPopularProductTagsOnHomepage", true, 2, true);

        //    ////await Services.SettingFactory.SaveSettingsAsync(new TestSettings(), 1);
        //    ////await _db.SaveChangesAsync();

        //    ////await Services.Settings.ApplySettingAsync("yodele.gut", "yodele");
        //    ////await Services.Settings.ApplySettingAsync("yodele.schlecht", "yodele");
        //    ////await Services.Settings.ApplySettingAsync("yodele.prop3", "yodele");
        //    ////await Services.Settings.ApplySettingAsync("yodele.prop4", "yodele");
        //    ////await _db.SaveChangesAsync();

        //    ////var yodele1 = await Services.Settings.GetSettingByKeyAsync<string>("yodele.gut");
        //    ////var yodele2 = await Services.Settings.GetSettingByKeyAsync<string>("yodele.schlecht");
        //    ////var yodele3 = await Services.Settings.GetSettingByKeyAsync<string>("yodele.prop3");
        //    ////var yodele4 = await Services.Settings.GetSettingByKeyAsync<string>("yodele.prop4");
        //    //////await Services.Settings.DeleteSettingsAsync("yodele");
        //    ////yodele1 = await Services.Settings.GetSettingByKeyAsync<string>("yodele.gut");

        //    ////await _db.SaveChangesAsync();

        //    //var testSettings = await Services.SettingFactory.LoadSettingsAsync<TestSettings>(1);
        //    //testSettings.Prop1 = CommonHelper.GenerateRandomDigitCode(10);
        //    //testSettings.Prop2 = CommonHelper.GenerateRandomDigitCode(10);
        //    //testSettings.Prop3 = CommonHelper.GenerateRandomDigitCode(10);
        //    //var numSaved = await Services.SettingFactory.SaveSettingsAsync(testSettings, 1);
        //    #endregion

        //    //_cancelTokenSource = new CancellationTokenSource();
        //    //await _asyncState.CreateAsync(new MyProgress(), cancelTokenSource: _cancelTokenSource);

        //    //var result = await Services.Resolve<IDbLogService>().ClearLogsAsync(new DateTime(2016, 12, 31), LogLevel.Fatal);

        //    //var count = await _db.Countries
        //    //    .AsNoTracking()
        //    //    .Where(x => x.SubjectToVat)
        //    //    .AsCaching()
        //    //    .CountAsync();

        //    //var langService = Services.Resolve<ILanguageService>();
        //    //for (var i = 0; i < 50; i++)
        //    //{
        //    //    var lid = await langService.GetDefaultLanguageIdAsync();
        //    //    var storeCache = _storeContext.GetCachedStores();
        //    //    var anon = await _db.Countries
        //    //        .AsNoTracking()
        //    //        .Where(x => x.SubjectToVat == true && x.DisplayOrder > 0)
        //    //        .AsCaching()
        //    //        .Select(x => new { x.Id, x.Name, x.TwoLetterIsoCode })
        //    //        .ToListAsync();
        //    //}

        //    //var anon = await _db.Countries
        //    //    .AsNoTracking()
        //    //    .Where(x => x.SubjectToVat == true && x.DisplayOrder > 0)
        //    //    .AsCaching()
        //    //    .Select(x => new { x.Id, x.Name, x.TwoLetterIsoCode })
        //    //    .ToListAsync();

        //    //var anon2 = _db.Countries
        //    //    .AsNoTracking()
        //    //    .Where(x => x.SubjectToVat == true && x.DisplayOrder > 1)
        //    //    .AsCaching()
        //    //    //.Select(x => new { x.Id, x.Name, x.TwoLetterIsoCode })
        //    //    .ToList();

        //    //var noResult = _db.Countries
        //    //    .AsNoTracking()
        //    //    .Where(x => x.Name == "fsdfsdfsdfsfsdfd")
        //    //    .AsCaching()
        //    //    //.Select(x => new { x.Id, x.Name, x.TwoLetterIsoCode })
        //    //    .FirstOrDefault();

        //    await Task.Delay(1);

        //    var storeId = _storeContext.CurrentStore.Id;

        //    ViewBag.MetaTitle = _homePageSettings.GetLocalizedSetting(x => x.MetaTitle, storeId);
        //    ViewBag.MetaDescription = _homePageSettings.GetLocalizedSetting(x => x.MetaDescription, storeId);
        //    ViewBag.MetaKeywords = _homePageSettings.GetLocalizedSetting(x => x.MetaKeywords, storeId);

        //    return View();
        //}

        [GdprConsent]
        [LocalizedRoute("/controls", Name = "Controls")]
        public async Task<IActionResult> Controls()
        {
            var testModel = new TestModel { TestProp1 = "Hello", TestProp2 = "World", TestProp4 = true };
            testModel.Locales.Add(new LocalizedTestModel { LanguageId = 1, TestProp1 = "Hello 1", TestProp2 = "Word 1" });
            testModel.Locales.Add(new LocalizedTestModel { LanguageId = 2, TestProp1 = "Hello 2", TestProp2 = "Word 2" });

            //var query = new MediaSearchQuery
            //{
            //    FolderId = 3,
            //    DeepSearch = true,
            //    ExactMatch = true,
            //    IncludeAltForTerm = false,
            //    PageSize = 100
            //};

            //var matches = await _mediaService.SearchFilesAsync(query);
            //Request.Query.TryGetValue("page", out var pagenumber);
            //matches.PageNumber = Convert.ToInt32(pagenumber);

            //testModel.TestList = matches;

            await Task.Delay(1);

            return View(testModel);
        }

        [LocalizedRoute("/privacy", Name = "Privacy")]
        public async Task<IActionResult> Privacy()
        {
            #region Settings Test
            //var xxx = await Services.Settings.GetSettingByKeyAsync<bool>("CatalogSettings.ShowPopularProductTagsOnHomepage", true, 2, true);

            //var yodele1 = await Services.Settings.GetSettingByKeyAsync<string>("yodele.gut");
            //var yodele2 = await Services.Settings.GetSettingByKeyAsync<string>("yodele.schlecht");
            //var yodele3 = await Services.Settings.GetSettingByKeyAsync<string>("yodele.prop3");
            //var yodele4 = await Services.Settings.GetSettingByKeyAsync<string>("yodele.prop4");
            ////await Services.Settings.DeleteSettingsAsync("yodele");
            //yodele1 = await Services.Settings.GetSettingByKeyAsync<string>("yodele.gut");

            //var testSettings = await Services.SettingFactory.LoadSettingsAsync<TestSettings>(1);
            #endregion

            //await _asyncState.UpdateAsync<MyProgress>(x =>
            //{
            //    x.Percent++;
            //    x.Message = $"Fortschritt {x.Percent}";
            //});

            //var numDeleted = await _locService.DeleteLocaleStringResourcesAsync("Yodele.Nixda");

            await using var scope = await _db.OpenConnectionAsync();

            var products = await _db.Products.OrderByDescending(x => x.Id).Skip(600).Take(100).ToListAsync();
            var urlService = Services.Resolve<IUrlService>();

            //foreach (var product in products)
            //{
            //    var result = await urlService.ValidateSlugAsync(product, product.Name, true);
            //    //await urlService.ApplySlugAsync(result, false);
            //}

            //using var batchScope = urlService.CreateBatchScope();
            //foreach (var product in products)
            //{
            //    batchScope.ApplySlugs(new ValidateSlugResult { Source = product, Slug = product.BuildSlug() });
            //}
            //await batchScope.CommitAsync();

            //int numSaved = await _db.SaveChangesAsync();

            return View();
        }

        [LocalizedRoute("/countries")]
        public async Task<IActionResult> Countries()
        {
            #region Test

            var taxSettings = await _settingFactory.LoadSettingsAsync<TaxSettings>(_storeContext.CurrentStore.Id);

            //_cache.Put("a", new CacheEntry { Key = "a", Value = "a" });
            //_cache.Put("b", new CacheEntry { Key = "b", Value = "b", Dependencies = new[] { "a" } });
            //_cache.Put("c", new CacheEntry { Key = "c", Value = "c", Dependencies = new[] { "a", "b" } });
            //_cache.Put("d", new CacheEntry { Key = "d", Value = "d", Dependencies = new[] { "a", "b", "c" } });

            ////_cache.Remove("d");
            ////_cache.Remove("c");
            //_cache.Remove("b");
            ////_cache.Remove("a");

            #endregion

            Logger.Error(new Exception("WTF Exception"), "WTF maaaan");
            Logger.Warn("WTF WARN maaaan");

            Logger.Info("INFO maaaan");
            _logger1.Info("INFO maaaan");
            _logger2.Info("INFO maaaan");
            _logger2.Warn("WARN maaaan");
            //_logger2.Error("WARN maaaan");

            //_asyncState.Cancel<MyProgress>();
            ////_cancelTokenSource.Cancel();
            //_cancelTokenSource = new CancellationTokenSource();

            var query = _db.Countries
                .AsNoTracking()
                .ApplyStoreFilter(1)
                .Include(x => x.StateProvinces)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name);

            //var countries = await GetCountriesCached();
            //countries = await GetCountriesCached();
            //countries = await GetCountriesCached();
            //countries = await GetCountriesCached();
            //countries = await GetCountriesCached();
            //countries = await GetCountriesCached();

            //var countries = await GetCountriesUncached();
            //countries = await GetCountriesUncached();
            //countries = await GetCountriesUncached();
            //countries = await GetCountriesUncached();
            //countries = await GetCountriesUncached();
            //countries = await GetCountriesUncached();

            //var country = GetCountryCachedSync();
            //country = GetCountryCachedSync();
            //country = GetCountryCachedSync();

            var countries = await GetCountries();
            for (var i = 0; i < 10; i++)
            {
                var country = await GetCountry();
            }

            //_db.SaveChanges();

            return View(countries);
        }

        [Route("settings")]
        public async Task<IActionResult> Settings()
        {
            await _asyncState.RemoveAsync<MyProgress>();

            var settings = await _db.Settings
                .AsNoTracking()
                .ApplySorting()
                .Take(500)
                .ToListAsync();

            #region Test

            var p = _db.DataProvider;

            //await p.BackupDatabaseAsync(@"D:\_Backup\db\yoman.bak");
            //await p.RestoreDatabaseAsync(@"D:\_Backup\db\yoman.bak");

            //var x = p.HasTable("Product");
            //var y = await p.HasTableAsync("xxxxxProduct");
            //var z = p.HasDatabase("yogisan-db");
            //z = await p.HasDatabaseAsync("FelgenOnline");
            //z = p.HasDatabase("yodeleeeeeee");
            //z = p.HasColumn("Discount", "Name");
            //z = await p.HasColumnAsync("Discount", "xxxxxName");

            //var ident = p.GetTableIdent<Store>();
            //ident = await p.GetTableIdentAsync<Country>();
            //ident = p.GetTableIdent<Setting>();

            //var size = p.GetDatabaseSize();
            //////await p.ShrinkDatabaseAsync();
            ////p.ReIndexTables();
            ////p.ShrinkDatabase();
            ////size = p.GetDatabaseSize();

            return View(settings);

            //var attrs = new GenericAttribute[] 
            //{
            //    new GenericAttribute { EntityId = 1, Key = "Yo", KeyGroup = "Man", StoreId = 1, Value = "Wert" },
            //    new GenericAttribute { EntityId = 1, Key = "Yo", KeyGroup = "Man", StoreId = 1, Value = "Wert" },
            //    new GenericAttribute { EntityId = 1, Key = "Yo", KeyGroup = "Man", StoreId = 1, Value = "Wert" },
            //    new GenericAttribute { EntityId = 1, Key = "Yo", KeyGroup = "Man", StoreId = 1, Value = "Wert" }
            //};
            //var maps = new StoreMapping[]
            //{
            //    new StoreMapping { EntityId = 1, EntityName = "Product", StoreId = 1 },
            //    new StoreMapping { EntityId = 1, EntityName = "Product", StoreId = 1 },
            //    new StoreMapping { EntityId = 1, EntityName = "Product", StoreId = 1 },
            //    new StoreMapping { EntityId = 1, EntityName = "Product", StoreId = 1 },
            //    new StoreMapping { EntityId = 1, EntityName = "Product", StoreId = 1 },
            //    new StoreMapping { EntityId = 1, EntityName = "Product", StoreId = 1 }
            //};

            //_db.GenericAttributes.AddRange(attrs);
            //_db.StoreMappings.AddRange(maps);

            //await _db.SaveChangesAsync();

            //_db.GenericAttributes.RemoveRange(attrs);
            //_db.StoreMappings.RemoveRange(maps);

            //_db.SaveChanges();

            #endregion
        }

        [LocalizedRoute("/logs")]
        public async Task<IActionResult> Logs()
        {
            #region Test

            //var logger = _loggerFactory.CreateLogger("File");
            //logger.Debug("Yodeleeeee");
            //logger.Info("Yodeleeeee");
            //logger.Warn("Yodeleeeee");
            //logger.Error("Yodeleeeee");

            //logger = _loggerFactory.CreateLogger("File/App_Data/Logs/yodele/");
            //logger.Debug("Yodeleeeee");
            //logger.Info("Yodeleeeee");
            //logger.Warn("Yodeleeeee");
            //logger.Error("Yodeleeeee");

            //logger = _loggerFactory.CreateLogger("File/App_Data/Logs/hello");
            //logger.Debug("Yodeleeeee");
            //logger.Info("Yodeleeeee");
            //logger.Warn("Yodeleeeee");
            //logger.Error("Yodeleeeee");

            #endregion

            var query = _db.Logs
                .AsNoTracking()
                .Where(x => x.CustomerId != 399003)
                .OrderByDescending(x => x.CreatedOnUtc)
                .Take(500);

            var logs = await query.ToListAsync();

            var country = await _db.Countries.OrderByDescending(x => x.Id).FirstOrDefaultAsync();
            country.DisplayCookieManager = !country.DisplayCookieManager;
            await _db.SaveChangesAsync();

            return View(logs);
        }

        [Route("/files")]
        public async Task<IActionResult> Files()
        {
            var mediaService = HttpContext.RequestServices.GetRequiredService<IMediaService>();

            var images = (await _db.MediaFiles
                .AsNoTracking()
                .Where(x => x.MediaType == MediaType.Image && x.Extension != "svg" && x.Size > 0 && x.FolderId != null /*&& x.Extension == "jpg" && x.Size < 10000*/)
                //.OrderByDescending(x => x.Size)
                .Take(100)
                .ToListAsync())
                .Select(x => mediaService.ConvertMediaFile(x))
                .ToList();

            //var videos = (await _db.MediaFiles
            //    .AsNoTracking()
            //    .Where(x => x.MediaType == MediaType.Video && x.FolderId != null)
            //    .Take(20)
            //    .ToListAsync())
            //    .Select(x => mediaService.ConvertMediaFile(x))
            //    .ToList();

            //var audios = (await _db.MediaFiles
            //    .AsNoTracking()
            //    .Where(x => x.MediaType == MediaType.Audio && x.FolderId != null)
            //    .Take(20)
            //    .ToListAsync())
            //    .Select(x => mediaService.ConvertMediaFile(x))
            //    .ToList();

            var files = new List<MediaFileInfo>();
            //files.AddRange(videos);
            //files.AddRange(audios);
            files.AddRange(images);

            return View(files);
        }

        [Route("imaging")]
        public async Task<IActionResult> ImagingTest()
        {
            var tempPath = "D:\\_temp\\_ImageSharp";

            //var urlHelper = HttpContext.RequestServices.GetRequiredService<IUrlHelper>();
            //var yes = urlHelper.RouteUrl("Product", new { SeName = "yodele", culture = "de" }, "http");

            //await Task.Delay(50);
            //return Content(yes);

            var files = await _db.MediaFiles
                .AsNoTracking()
                .Where(x => x.MediaType == "image" && x.Size > 0 /*&& x.Extension == "jpg" && x.Size < 10000*/)
                .OrderByDescending(x => x.Size)
                //.OrderBy(x => x.Size)
                .Take(1)
                .ToListAsync();

            //Save originals
            foreach (var file in files)
            {
                var outPath = System.IO.Path.Combine(tempPath, System.IO.Path.GetFileNameWithoutExtension(file.Name) + "-orig." + file.Extension);
                using (var outFile = new FileStream(outPath, FileMode.Create))
                {
                    using (var inStream = await _mediaStorageProvider.OpenReadAsync(file))
                    {
                        await inStream.CopyToAsync(outFile);
                    }
                }
            }

            long len = 0;
            var watch = new Stopwatch();
            watch.Start();

            foreach (var file in files)
            {
                using (var inStream = await _mediaStorageProvider.OpenReadAsync(file))
                {
                    var image = await _imageFactory.LoadAsync(inStream);

                    image.Transform(x =>
                    {
                        x.Resize(new ResizeOptions
                        {
                            Size = new Size(800, 800),
                            Mode = ResizeMode.Max,
                            Resampling = ResamplingMode.Bicubic
                        });

                        //x.OilPaint(40, 30);
                        //x.Pad(1200, 900, Color.Bisque);
                        x.Sepia();
                    });

                    if (image.Format is IJpegFormat jpeg)
                    {
                        jpeg.Quality = 90;
                        jpeg.Subsample = JpegSubsample.Ratio420;
                    }
                    else if (image.Format is IPngFormat png)
                    {
                        //png.ChunkFilter = PngChunkFilter.ExcludeAll;
                        //png.ColorType = PngColorType.Grayscale;
                        png.CompressionLevel = PngCompressionLevel.BestCompression;
                        png.QuantizationMethod = QuantizationMethod.Wu;
                        //png.BitDepth = PngBitDepth.Bit8;
                        //png.ColorType = PngColorType.Palette;
                    }

                    var outPath = System.IO.Path.Combine(tempPath, file.Name);
                    using (var outFile = new FileStream(outPath, FileMode.Create))
                    {
                        await image.SaveAsync(outFile);
                        len += outFile.Length;
                    }
                }
            }

            watch.Stop();
            var msg = $"Images: {files.Count}. Duration: {watch.ElapsedMilliseconds} ms., Size: {len.Bytes().Humanize()}, SIMD: {Vector.IsHardwareAccelerated}";

            _imageFactory.ReleaseMemory();

            return Content(msg);
        }

        [Route("messagemodel")]
        public async Task<IActionResult> Messages()
        {
            var model = new TestModelMH();

            model.CampaingCount = await _db.Campaigns.AsNoTracking().CountAsync();

            var campaignService = Services.Resolve<ICampaignService>();
            var campaign = await _db.Campaigns.AsNoTracking().FirstOrDefaultAsync();

            // TODO: (mh) (core) Test again when LiquidTemplateEngine is available.
            //var test = await campaignService.PreviewAsync(campaign);

            return View(model);
        }

        [Route("env")]
        public IActionResult Env()
        {
            var vars = Environment.GetEnvironmentVariables();
            var dict = new Dictionary<string, string>();
            var cfg = HttpContext.RequestServices.GetService<IApplicationContext>().Configuration.AsEnumerable();

            foreach (var key in vars.Keys)
            {
                dict[key.ToString()] = vars[key].ToString();
            }

            foreach (var kvp in cfg)
            {
                dict[kvp.Key] = kvp.Value;
            }

            return View(dict);
        }

        private Task<List<Country>> GetCountries()
        {
            var query = _db.Countries
                .AsNoTracking()
                .ApplyStoreFilter(1)
                .Include(x => x.StateProvinces)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name);

            return query.ToListAsync();
        }

        private Task<Country> GetCountry()
        {
            var query = _db.Countries
                .AsNoTracking()
                .ApplyStoreFilter(1)
                .Include(x => x.StateProvinces)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name);

            return query.FirstOrDefaultAsync();
        }

        private Country GetCountryCachedSync()
        {
            var query = _db.Countries
                .AsNoTracking()
                .ApplyStoreFilter(1)
                .Include(x => x.StateProvinces)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name);

            return query.FirstOrDefault();
        }

        private List<Country> GetCountriesUncachedSync()
        {
            var query = _db.Countries
                .AsNoTracking()
                .ApplyStoreFilter(1)
                .Include(x => x.StateProvinces)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name);

            return query.ToList();
        }

        private List<Country> GetCountriesCachedSync()
        {
            var query = _db.Countries
                .AsNoTracking()
                .ApplyStoreFilter(1)
                .Include(x => x.StateProvinces)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name);

            return query.ToList();
        }

        public IActionResult Slug()
        {
            var e = (UrlRecord)HttpContext.GetRouteData().Values["entity"];
            return Content($"Slug matched >>> Entity: {e.EntityName} {e.EntityId}, Id: {e.Id}, Language: {e.LanguageId}, Slug: {e.Slug}, IsActive: {e.IsActive}");
        }

        public async Task<IActionResult> MhTest()
        {
            //// QuantityUnit
            //// Get QuantityUnit by Id
            //var qu = _db.QuantityUnits.ApplyQuantityUnitFilter(1).FirstOrDefault();

            //// Save hook > TODO: BROKEN > Why?
            //qu.IsDefault = true;
            //_db.SaveChanges();
            //// TODO Test: Assert.OnlyOne has Default = true, 

            //// Delete hook
            //var qu2 = _db.QuantityUnits.ApplyQuantityUnitFilter(22).FirstOrDefault();

            //if (qu2 != null)
            //{
            //    _db.QuantityUnits.Remove(qu2);
            //    await _db.SaveChangesAsync();
            //}

            //// StateProvince
            //var sp = _db.StateProvinces
            //    .ApplyCountryFilter(1)
            //    .ApplyAbbreviationFilter("BE")
            //    .FirstOrDefault();
            //// TODO Test: Assert name of entity is Berlin

            //// DeliveryTime
            ////var dt = _db.DeliveryTimes.GetDeliveryTimeFilter(1);

            //var menuItems = await _db.MenuItems
            //    .AsNoTracking()
            //    .ApplyMenuFilter(6, "")
            //    .ToListAsync();

            return Content("");
        }

        public async Task<IActionResult> MsTest()
        {
            //var product = _db.Products.FindByIdAsync(4).Await();

            //var taxRate = await _taxService.GetTaxRateAsync(product);

            //var x = 10;
            //    var xxx = await _db.OrderItems.SelectAlsoPurchasedProductIds(11).ToListAsync();

            //    var aaa = 10;

            //var customer = await _db.Customers.Where(x => x.Email == "admin@meinstore.de").FirstOrDefaultAsync();

            //var xxxxxx = await _giftCardService.GetValidGiftCardsAsync(customer: customer);
            //var sdsdsd = (GiftCardCouponCode)xxxxxx.FirstOrDefault().GiftCard.GiftCardCouponCode;
            //var customerCart = await _cartService.GetCartItemsAsync(customer, ShoppingCartType.ShoppingCart);

            //var xxxx = await _shippingService.GetCartTotalWeightAsync(customerCart);

            //var sddsds = await _orderCalculationService.GetShoppingCartSubTotalAsync(customerCart);
            //var xxx = await _orderCalculationService.GetShoppingCartTotalAsync(customerCart);
            //var result = await _shippingService.GetCartTotalWeightAsync(customerCart);

            // GetAllProviders throws....
            //var shippingOptions = _shippingService.GetShippingOptions(customerCart, Services.WorkContext.CurrentCustomer.ShippingAddress);

            //var rawCheckoutAttributes = Services.WorkContext.CurrentCustomer.GenericAttributes.CheckoutAttributes;
            //var formatted = _checkoutAttributeFormatter.FormatAttributesAsync(new(rawCheckoutAttributes));

            //var giftCards = await _giftCardService.GetValidGiftCardsAsync();

            return Content("");
        }

        #endregion

        //[Permission("MyTestPermission")]
        //[ValidateAdminIpAddress]
        public async Task<IActionResult> MgTest(/*CatalogSearchQuery query*//*ProductVariantQuery query*/)
        {
            var content = new StringBuilder();
            //var productIds = new int[] { 4317, 1748, 1749, 1750, 4317, 4366 };

            var cs = Services.Resolve<ICurrencyService>();
            var ocs = Services.Resolve<IOrderCalculationService>();
            var scs = Services.Resolve<IShoppingCartService>();
            var pcs = Services.Resolve<IPriceCalculationService>();
            var store = Services.StoreContext.CurrentStore;
            var currency = Services.WorkContext.WorkingCurrency;
            var customer = await _db.Customers.FindByIdAsync(2666330, false);
            var cart = await scs.GetCartItemsAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
            var subtotal = await ocs.GetShoppingCartSubtotalAsync(cart);
            var subtotalConverted = cs.ConvertFromPrimaryCurrency(subtotal.SubtotalWithoutDiscount.Amount, currency);

            foreach (var item in subtotal.LineItems)
            {
                content.AppendLine($"{item.Item.Item.Id} {item.UnitPrice.FinalPrice.Amount} {item.Subtotal.FinalPrice.Amount}");
            }
            content.AppendLine("--------------------------------------------------------------");
            content.AppendLine($"{subtotal.SubtotalWithoutDiscount.Amount} {subtotalConverted.Amount}");


            var tenantRoot = Data.DataSettings.Instance.TenantRoot;
            var exportProfiles = tenantRoot.GetDirectory("ExportProfiles");
            var folderName = exportProfiles.SubPath + "/" + tenantRoot.CreateUniqueDirectoryName(CommonHelper.MapPath(exportProfiles.SubPath), "My test folder");
            content.AppendLine();
            content.AppendLine(folderName);

            var product = await _db.Products.FindByIdAsync(1751, false);
            var calculationOptions = pcs.CreateDefaultOptions(true, customer, currency);
            var productPrice = await pcs.CalculatePriceAsync(new PriceCalculationContext(product, calculationOptions));
            var basePriceInfo = pcs.GetBasePriceInfo(product, productPrice.FinalPrice, currency, null, false);
            content.AppendLine("");
            content.AppendLine("Base price: " + basePriceInfo);

            content.AppendLine("");
            var query1 = GetEntitiesQuery(typeof(Product), null);
            query1 = ApplyPaging(query1, 2, 2);
            var entities1 = await query1.ToListAsync();
            var products = entities1.Cast<Product>();
            products.Each(x => content.AppendLine($"{x.Id} {x.Name}"));

            content.AppendLine("");
            var query2 = GetEntitiesQuery(typeof(Category), new[] { 152, 153, 154, 155 });
            var entities2 = await query2.ToListAsync();
            var categories = entities2.Cast<Category>();
            categories.Each(x => content.AppendLine($"{x.Id} {x.Name}"));

            return Content(content.ToString());
            //return View();

            IQueryable<BaseEntity> GetEntitiesQuery(Type type, int[] ids)
            {
                IQueryable<BaseEntity> query = null;

                if (type == typeof(Product))
                {
                    var productQuery = _db.Products.AsNoTracking();
                    productQuery = productQuery.Where(x => x.Published && x.ProductTypeId == 5);

                    query = productQuery;
                }
                else if (type == typeof(Category))
                {
                    var categoryQuery = _db.Categories.AsNoTracking();
                    categoryQuery = categoryQuery.Where(x => x.Published);

                    query = categoryQuery;
                }

                if (ids?.Any() ?? false)
                {
                    query = query.Where(x => ids.Contains(x.Id));
                }

                return query;
            }

            IQueryable<BaseEntity> ApplyPaging(IQueryable<BaseEntity> query, int? skip, int take)
            {
                query = query.OrderBy(x => x.Id);

                if (skip.HasValue)
                {
                    query = query.Skip(skip.Value);
                }

                if (take != int.MaxValue)
                {
                    query = query.Take(take);
                }

                return query;
            }
        }
    }
}
