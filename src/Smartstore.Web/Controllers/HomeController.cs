using System.Data;
using Autofac;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Common.Configuration;
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
using Smartstore.Net.Mail;
using Smartstore.Utilities.Html;
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

        public HomeController(
            SmartDbContext db,
            IStoreContext storeContext,
            HomePageSettings homePageSettings,
            IMessageFactory messageFactory,
            PrivacySettings privacySettings,
            CommonSettings commonSettings,
            StoreInformationSettings storeInformationSettings)
        {
            _db = db;
            _storeContext = storeContext;
            _homePageSettings = homePageSettings;
            _messageFactory = messageFactory;
            _privacySettings = privacySettings;
            _commonSettings = commonSettings;
            _storeInformationSettings = storeInformationSettings;
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
    }
}