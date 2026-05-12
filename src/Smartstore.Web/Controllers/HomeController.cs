using System.Data;
using Autofac;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Content.Topics;
using Smartstore.Core.DataExchange.Export;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Logging;
using Smartstore.Core.Messaging;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Json;
using Smartstore.Net.Mail;
using Smartstore.Utilities.Html;
using Smartstore.Web.Infrastructure.Hooks;
using Smartstore.Web.Models.Common;

namespace Smartstore.Web.Controllers
{
    public class HomeController : PublicController
    {
        private readonly SmartDbContext _db;
        private readonly IStoreContext _storeContext;
        private readonly HomePageSettings _homePageSettings;
        private readonly IMessageFactory _messageFactory;
        private readonly PrivacySettings _privacySettings;
        private readonly CommonSettings _commonSettings;
        private readonly StoreInformationSettings _storeInformationSettings;
        private readonly ContactDataSettings _contactDataSettings;
        private readonly CompanyInformationSettings _companySettings;
        private readonly SocialSettings _socialSettings;
        private readonly IPageAssetBuilder _assetBuilder;
        private readonly IMediaService _mediaService;

        public HomeController(
            SmartDbContext db,
            IStoreContext storeContext,
            HomePageSettings homePageSettings,
            IMessageFactory messageFactory,
            PrivacySettings privacySettings,
            CommonSettings commonSettings,
            StoreInformationSettings storeInformationSettings,
            ContactDataSettings contactDataSettings,
            CompanyInformationSettings companySettings,
            SocialSettings socialSettings,
            IPageAssetBuilder assetBuilder,
            IMediaService mediaService)
        {
            _db = db;
            _storeContext = storeContext;
            _homePageSettings = homePageSettings;
            _messageFactory = messageFactory;
            _assetBuilder = assetBuilder;
            _mediaService = mediaService;
            _assetBuilder = assetBuilder;
            _privacySettings = privacySettings;
            _commonSettings = commonSettings;
            _storeInformationSettings = storeInformationSettings;
            _contactDataSettings = contactDataSettings;
            _companySettings = companySettings;
            _socialSettings = socialSettings;
        }

        [LocalizedRoute("/", Name = "Homepage")]
        public async Task<IActionResult> Index()
        {
            var storeId = _storeContext.CurrentStore.Id;

            ViewBag.MetaTitle = _homePageSettings.GetLocalizedSetting(x => x.MetaTitle, storeId);
            ViewBag.MetaDescription = _homePageSettings.GetLocalizedSetting(x => x.MetaDescription, storeId);
            ViewBag.MetaKeywords = _homePageSettings.GetLocalizedSetting(x => x.MetaKeywords, storeId);

            await CreateHomepageJsonLdAsync();

            return View();
        }

        [CheckStoreClosed(false)]
        [LocalizedRoute("/storeclosed", Name = "StoreClosed")]
        public IActionResult StoreClosed()
        {
            if (!_storeInformationSettings.StoreClosed)
            {
                return RedirectToRoute("Homepage");
            }

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
                MetaKeywords = topic?.GetLocalized(x => x.MetaKeywords),
                MetaDescription = topic?.GetLocalized(x => x.MetaDescription),
                MetaTitle = topic?.GetLocalized(x => x.MetaTitle),
            };

            return View(model);
        }

        [HttpPost, ActionName("ContactUs")]
        [ValidateCaptcha(CaptchaSettings.Targets.ContactUs)]
        [ValidateHoneypot, GdprConsent]
        [LocalizedRoute("/contactus", Name = "ContactUs")]
        public async Task<IActionResult> ContactUsSend(ContactUsModel model)
        {
            if (ModelState.IsValid)
            {
                var customer = Services.WorkContext.CurrentCustomer;
                var email = model.Email.Trim();
                var fullName = model.FullName;
                var subject = T("ContactUs.EmailSubject", Services.StoreContext.CurrentStore.Name);
                var body = HtmlUtility.ConvertPlainTextToHtml(model.Enquiry.HtmlEncode());

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

            return View(model);
        }

        public IActionResult Sitemap()
        {
            return RedirectPermanent(Services.StoreContext.CurrentStore.GetBaseUrl());
        }

        // TODO: (jsonld) Cache this somehow? It changes very rarely but is called very often.
        private async Task CreateHomepageJsonLdAsync()
        {
            var store = _storeContext.CurrentStore;
            var storeUrl = _storeContext.CurrentStore.Url.EnsureEndsWith("/");

            // Logo
            var cacheKey = "pres:logo-json-ld-{0}-{1}-{2}".FormatInvariant(store.Id, Request.Scheme, Request.Host);
            var logo = await Services.Cache.GetAsync(cacheKey, async (o) =>
            {
                o.ExpiresIn(TimeSpan.FromHours(4));

                var logoFile = await _mediaService.GetFileByIdAsync(store.LogoMediaFileId, MediaLoadFlags.AsNoTracking);

                if (logoFile == null)
                {
                    return null;
                }
                
                return JsonLdFragment.Create("ImageObject", new
                {
                    url = _mediaService.GetUrl(logoFile, 0, store.GetBaseUrl(), false),
                    width = logoFile.Size.Width,
                    height = logoFile.Size.Height
                });
            });

            string countryCode = null;
            if (_companySettings.CountryId != 0)
            {
                // TODO: (jsonld) Is caching really necessary here? Note, Country is a CacheableEntity.
                countryCode = await Services.Cache.GetAsync($"jsonld:country{_companySettings.CountryId}", async ctx =>
                {
                    ctx.ExpiresIn(TimeSpan.FromHours(4));

                    var country = await _db.Countries
                        .Select(x => new { x.Id, x.TwoLetterIsoCode })
                        .FirstOrDefaultAsync(x => x.Id == _companySettings.CountryId);

                    return country?.TwoLetterIsoCode.NullEmpty();
                });
            }

            // TODO: (jsonld) Only add values that are actually set.
            var contactPoint = JsonLdFragment.Create("ContactPoint", new
            {
                telephone = _contactDataSettings.HotlineTelephoneNumber ?? _contactDataSettings.CompanyTelephoneNumber,
                email = _contactDataSettings.ContactEmailAddress,
                contactType = "customer service",
                areaServed = countryCode
            });

            var address = JsonLdFragment.Create("PostalAddress", new
            {
                streetAddress = _companySettings.Street,
                addressLocality = _companySettings.City,
                postalCode = _companySettings.ZipCode,
                addressCountry = countryCode
            });

            var links = new[]
            {
                _socialSettings.FacebookLink,
                _socialSettings.TwitterLink,
                _socialSettings.InstagramLink,
                _socialSettings.TikTokLink,
                _socialSettings.YoutubeLink,
                _socialSettings.VimeoLink,
                _socialSettings.PinterestLink,
                _socialSettings.SnapchatLink,
                _socialSettings.FlickrLink,
                _socialSettings.LinkedInLink,
                _socialSettings.XingLink,
                _socialSettings.TumblrLink,
                _socialSettings.ElloLink,
                _socialSettings.BehanceLink
            };

            var sameAs = new List<string>(links.Where(x => x.HasValue()));

            _assetBuilder.JsonLd.Organization
                .Prop("@id", storeUrl + "#organization")
                .Prop("name", store.Name)
                .Prop("url", storeUrl)
                .Obj("contactPoint", contactPoint)
                .Obj("address", address)
                .Arr("sameAs", sameAs);

            if (logo != null)
            {
                _assetBuilder.JsonLd.Organization.Obj("logo", logo);
            }

            _assetBuilder.JsonLd.WebSite
                .Prop("@id", storeUrl.EnsureEndsWith("/") + "#website")
                .Prop("name", store.Name)
                .Prop("url", store.Url)
                .Obj("potentialAction", JsonLdFragment.Create("SearchAction", new Dictionary<string, object>
                {
                    ["target"] = JsonLdFragment.Create("EntryPoint", new
                    {
                        urlTemplate = storeUrl + "search?q={search_term_string}"
                    }),
                    ["query-input"] = "required name=search_term_string"
                }));
        }
    }
}