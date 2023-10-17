using Microsoft.AspNetCore.Http;
using Smartstore.Core.Common;
using Smartstore.Core.Configuration;

namespace Smartstore.Core.Identity
{
    /// <summary>
    /// Represents the cookie consent requirements.
    /// </summary>
    public enum CookieConsentRequirement
    {
        /// <summary>
        /// Cookie consent is never required.
        /// </summary>
        NeverRequired,

        /// <summary>
        /// Cookie consent is required if the visitor's resolved GEO location points to an EU country.
        /// For all other countries, cookie consent is disabled.
        /// </summary>
        RequiredInEUCountriesOnly,

        /// <summary>
        /// Cookie consent is required if the visitor's resolved country 
        /// is configured to require consent (see <see cref="Country.DisplayCookieManager"/>).
        /// </summary>
        DependsOnCountry
    }

    public class PrivacySettings : ISettings
    {
        /// <summary>
        /// Gets or sets the cookie consent requirement.
        /// </summary>
        public CookieConsentRequirement CookieConsentRequirement { get; set; } = CookieConsentRequirement.RequiredInEUCountriesOnly;

        /// <summary>
        /// Gets or sets a value indicating whether the cookie dialog will be display in a modal dialog.
        /// </summary>
        public bool ModalCookieConsent { get; set; } = true;

        /// <summary>
        /// Gets or sets the global SameSiteMode for cookies.
        /// </summary>
        public SameSiteMode SameSiteMode { get; set; } = SameSiteMode.Lax;

        /// <summary>
        /// Gets or sets the number of days after which visitor cookies of guests expire.
        /// </summary>
        public int VisitorCookieExpirationDays { get; set; } = 365;

        /// <summary>
        /// Gets or sets a value indicating whether to store last IP address for each customer.
        /// </summary>
        public bool StoreLastIpAddress { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to display a checkbox to the customer where he can agree to privacy terms.
        /// </summary>
        public bool DisplayGdprConsentOnForms { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the full name field is required on contact-us requests.
        /// </summary>
        public bool FullNameOnContactUsRequired { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the full name field is required on product requests.
        /// </summary>
        public bool FullNameOnProductRequestRequired { get; set; }

        /// <summary>
        /// Gets or sets cookie infos (JSON serialized).
        /// </summary>
        public string CookieInfos { get; set; } = string.Empty;
    }
}
