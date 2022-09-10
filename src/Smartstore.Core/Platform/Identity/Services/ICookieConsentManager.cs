namespace Smartstore.Core.Identity
{
    public partial interface ICookieConsentManager
    {
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
        bool IsCookieAllowed(CookieType cookieType);

        /// <summary>
        /// Gets cookie consent data.
        /// </summary>
        /// <returns>Cookie consent data.</returns>
        ConsentCookie GetCookieData();

        /// <summary>
        /// Sets the consent cookie.
        /// </summary>
        /// <param name="allowAnalytics">A value indicating whether analytical cookies are allowed to be set.</param>
        /// <param name="allowThirdParty">A value indicating whether third party cookies are allowed to be set.</param>
        void SetConsentCookie(bool allowAnalytics = false, bool allowThirdParty = false);
    }
}
