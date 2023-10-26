using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Identity;
using Smartstore.Core.Theming;
using Smartstore.Utilities;
using Smartstore.Web.Models.Common;

namespace Smartstore.Web.Components
{
    public class FooterViewComponent : SmartViewComponent
    {
        private readonly static string[] _hints = new string[] { "Shopsystem", "Onlineshop Software", "Shopsoftware", "E-Commerce Solution" };

        private readonly IThemeRegistry _themeRegistry;
        private readonly IWidgetProvider _widgetProvider;
        private readonly IDisplayHelper _displayHelper;
        private readonly ThemeSettings _themeSettings;
        private readonly CustomerSettings _customerSettings;
        private readonly TaxSettings _taxSettings;
        private readonly SocialSettings _socialSettings;
        private readonly PrivacySettings _privacySettings;

        public FooterViewComponent(
            IThemeRegistry themeRegistry,
            IWidgetProvider widgetProvider,
            IDisplayHelper displayHelper,
            ThemeSettings themeSettings,
            CustomerSettings customerSettings,
            TaxSettings taxSettings,
            SocialSettings socialSettings,
            PrivacySettings privacySettings)
        {
            _themeRegistry = themeRegistry;
            _widgetProvider = widgetProvider;
            _displayHelper = displayHelper;
            _themeSettings = themeSettings;
            _customerSettings = customerSettings;
            _taxSettings = taxSettings;
            _socialSettings = socialSettings;
            _privacySettings = privacySettings;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var store = Services.StoreContext.CurrentStore;
            var taxDisplayType = await Services.WorkContext.GetTaxDisplayTypeAsync(Services.WorkContext.CurrentCustomer, store.Id);
            var taxInfo = T(taxDisplayType == TaxDisplayType.IncludingTax ? "Tax.InclVAT" : "Tax.ExclVAT");
            var availableStoreThemes = !_themeSettings.AllowCustomerToSelectTheme
                ? new List<StoreThemeModel>()
                : _themeRegistry.GetThemeDescriptors()
                    .Select(x =>
                    {
                        return new StoreThemeModel
                        {
                            Name = x.Name,
                            Title = x.FriendlyName
                        };
                    })
                    .ToList();

            var model = new FooterModel
            {
                StoreName = store.Name,
                ShowLegalInfo = _taxSettings.ShowLegalHintsInFooter,
                ShowThemeSelector = availableStoreThemes.Count > 1,
                HideNewsletterBlock = _customerSettings.HideNewsletterBlock,
                ShowSocialLinks = _socialSettings.ShowSocialLinksInFooter
            };

            if (model.ShowSocialLinks)
            {
                TryAddSocialLink(_socialSettings.FacebookLink, "facebook-f", "Facebook");
                TryAddSocialLink(_socialSettings.TwitterLink, "x-twitter", "X (Twitter)");
                TryAddSocialLink(_socialSettings.InstagramLink, "instagram", "Instagram");
                TryAddSocialLink(_socialSettings.TikTokLink, "tiktok", "TikTok");
                TryAddSocialLink(_socialSettings.YoutubeLink, "youtube", "Youtube");
                TryAddSocialLink(_socialSettings.VimeoLink, "vimeo", "Vimeo");
                TryAddSocialLink(_socialSettings.PinterestLink, "pinterest-p", "Pinterest");
                TryAddSocialLink(_socialSettings.SnapchatLink, "snapchat", "Snapchat");
                TryAddSocialLink(_socialSettings.FlickrLink, "flickr", "Flickr");
                TryAddSocialLink(_socialSettings.LinkedInLink, "linkedin", "LinkedIn");
                TryAddSocialLink(_socialSettings.XingLink, "xing", "Xing");
                TryAddSocialLink(_socialSettings.TumblrLink, "tumblr", "Tumblr");
                TryAddSocialLink(_socialSettings.ElloLink, "ello", "Ello");
                TryAddSocialLink(_socialSettings.BehanceLink, "behance", "Behance");
            }

            var shippingInfoUrl = await Url.TopicAsync("ShippingInfo");
            if (shippingInfoUrl.HasValue())
            {
                model.LegalInfo = T("Tax.LegalInfoFooter", taxInfo, shippingInfoUrl);
            }
            else
            {
                model.LegalInfo = T("Tax.LegalInfoFooter2", taxInfo);
            }

            var hint = Services.Settings.GetSettingByKey("Rnd_SmCopyrightHint", string.Empty, store.Id);
            if (hint.IsEmpty())
            {
                hint = _hints[CommonHelper.GenerateRandomInteger(0, _hints.Length - 1)];

                await Services.Settings.ApplySettingAsync("Rnd_SmCopyrightHint", hint, store.Id);
                await Services.DbContext.SaveChangesAsync();
            }

            if (_displayHelper.DisplaySmartstoreHint())
            {
                model.SmartStoreHint = $"<a href='https://www.smartstore.com/' class='sm-hint' target='_blank'><strong>{hint}</strong></a> by SmartStore AG &copy; {DateTime.Now.Year}";
            }

            if (ShouldRenderGDPR())
            {
                _widgetProvider.RegisterViewComponent<GdprConsentViewComponent>("gdpr_consent_small", new { isSmall = true });
                HttpContext.Items["GdprConsentRendered"] = true;
            }

            return View(model);

            void TryAddSocialLink(string href, string cssClass, string displayName)
            {
                if (href.HasValue())
                {
                    model.AddSocialLink(href, cssClass, displayName);
                }
            }
        }

        private bool ShouldRenderGDPR()
        {
            if (!_privacySettings.DisplayGdprConsentOnForms)
                return false;

            if (HttpContext.Items.Keys.Contains("GdprConsentRendered"))
                return false;

            return true;
        }
    }
}
