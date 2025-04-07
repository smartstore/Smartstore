using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smartstore.Core.Identity
{
    public partial interface ICookieConsentManager
    {
        /// <summary>
        /// Gets A value indicating whether cookie consent is required for current user.
        /// </summary>
        /// <returns>A value indicating whether cookie consent is required for current user.</returns>
        Task<bool> IsCookieConsentRequiredAsync();

        /// <summary>
        /// Gets cookie infos from all modules that are publishing cookies to be displayed in the cookie manager dialog.
        /// </summary>
        /// <param name="withUserCookies"><c>true</c> to append user-defined cookie infos from <see cref="GetUserCookieInfos(bool)"/> call to the result list.</param>
        /// <returns>Cookie infos.</returns>
        Task<IList<CookieInfo>> GetCookieInfosAsync(bool withUserCookies = false);

        /// <summary>
        /// Loads and deserializes user-defined cookie infos from <see cref="PrivacySettings.CookieInfos"/> setting.
        /// </summary>
        /// <param name="translated">A value indicating whether to translate cookie display names and descriptions.</param>
        /// <returns>List of user-defined cookie infos.</returns>
        IReadOnlyList<CookieInfo> GetUserCookieInfos(bool translated = true);

        /// <summary>
        /// Gets a value indicating whether it is allowed to set a cookie of a certain type.
        /// </summary>
        /// <param name="cookieType">Type of the cookie.</param>
        /// <returns>A value indicating whether it is allowed to set a cookie of a certain type.</returns>
        Task<bool> IsCookieAllowedAsync(CookieType cookieType);

        /// <summary>
        /// Generates a script tag with the given src attribute. If the user has not consented to the given consent type, 
        /// the src attribute is moved to a data-src attribute and a data-consent attribute is added.
        /// </summary>
        /// <param name="consented">True if the user has consented to the given consent type, false otherwise.</param>
        /// <param name="consentType">The type of consent required to load the script.</param>
        /// <param name="src">The URL of the script to load.</param>
        /// <returns>The generated script tag.</returns>
        TagBuilder GenerateScript(bool consented, CookieType consentType, string src);

        /// <summary>
        /// Generates an inline script tag with the given code. If the user has not consented to the given consent type,
        /// the script's type will be <c>text/plain</c>.
        /// </summary>
        /// <param name="consented">True if the user has consented to the given consent type, false otherwise.</param>
        /// <param name="consentType">The type of consent required to load the script.</param>
        /// <param name="code">The code to be executed by the script. This should be a valid JavaScript code block.</param>
        /// <returns>The generated script tag.</returns>
        TagBuilder GenerateInlineScript(bool consented, CookieType consentType, string code);

        /// <summary>
        /// Gets cookie consent data.
        /// </summary>
        /// <returns>Cookie consent data.</returns>
        ConsentCookie GetCookieData();

        /// <summary>
        /// Sets the consent cookie.
        /// </summary>
        /// <param name="allowRequired">A value indicating whether required cookies are allowed to be set.</param>
        /// <param name="allowAnalytics">A value indicating whether analytical cookies are allowed to be set.</param>
        /// <param name="allowThirdParty">A value indicating whether third party cookies are allowed to be set.</param>
        /// <param name="adUserDataConsent">A value indicating whether sending of user data is allowed.</param>
        /// <param name="adPersonalizationConsent">A value indicating whether personalization is allowed.</param>
        Task<bool> SetConsentCookieAsync(
            bool allowRequired = false,
            bool allowAnalytics = false, 
            bool allowThirdParty = false, 
            bool adUserDataConsent = false, 
            bool adPersonalizationConsent = false);
    }
}
