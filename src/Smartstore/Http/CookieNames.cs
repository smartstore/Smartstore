namespace Smartstore.Net
{
    /// <summary>
    /// Cookie default names
    /// </summary>
    public static class CookieNames
    {
        /// <summary>
        /// Gets the cookie name prefix
        /// </summary>
        public static string Prefix => ".Smart";

        /// <summary>
        /// Gets the session cookie name.
        /// This cookie is used to maintain user session state across HTTP requests.
        /// </summary>
        public static string Session => Prefix + ".Session";

        /// <summary>
        /// Gets the TempData cookie name.
        /// This cookie is used to store temporary data between requests, typically for redirects.
        /// </summary>
        public static string TempData => Prefix + ".TempData";

        /// <summary>
        /// Gets the identity (registered account) cookie name.
        /// This cookie is used to authenticate and identify registered users across requests.
        /// </summary>
        public static string Identity => Prefix + ".Identity";

        /// <summary>
        /// Gets the identity external authentication cookie name.
        /// This cookie is used to manage authentication via external providers (e.g., Google, Facebook).
        /// </summary>
        public static string ExternalAuthentication => Prefix + ".ExternalAuth";

        /// <summary>
        /// Gets the visitor (guest account) cookie name.
        /// This cookie is used to track and identify guest users who are not registered.
        /// </summary>
        public static string Visitor => Prefix + ".Visitor";

        /// <summary>
        /// Gets the antiforgery cookie name.
        /// This cookie is used to protect against Cross-Site Request Forgery (CSRF) attacks.
        /// </summary>
        public static string Antiforgery => Prefix + ".Antiforgery";

        /// <summary>
        /// Gets the compared products cookie name.
        /// This cookie is used to store the list of products that the user wants to compare.
        /// </summary>
        public static string ComparedProducts => Prefix + ".ComparedProducts";

        /// <summary>
        /// Gets the recently viewed products cookie name.
        /// This cookie is used to track and display products that the user has recently viewed.
        /// </summary>
        public static string RecentlyViewedProducts => Prefix + ".RecentlyViewedProducts";

        /// <summary>
        /// Gets the GdprConsent cookie name.
        /// This cookie is used to store the user's GDPR consent status for data processing.
        /// </summary>
        public static string GdprConsent => Prefix + ".Consent";

        /// <summary>
        /// Gets the cookie name of the cookie consent manager.
        /// This cookie is used to store the user's cookie preferences and consent choices.
        /// </summary>
        public static string CookieConsent => Prefix + ".CookieConsent";

        /// <summary>
        /// Gets the StoreIdOverride cookie name.
        /// This cookie is used to override the default store selection in multi-store scenarios.
        /// </summary>
        public static string StoreIdOverride => Prefix + ".StoreIdOverride";

        /// <summary>
        /// Gets the PreviewModeOverride cookie name.
        /// This cookie is used to enable or disable preview mode for testing unpublished content.
        /// </summary>
        public static string PreviewModeOverride => Prefix + ".PreviewModeOverride";

        /// <summary>
        /// Gets the PreviewToolOpen cookie name.
        /// This cookie is used to remember the open/closed state of the preview tool panel.
        /// </summary>
        public static string PreviewToolOpen => Prefix + ".PreviewToolOpen";

        /// <summary>
        /// Gets the UserThemeChoice cookie name.
        /// This cookie is used to store the user's preferred theme selection (e.g., light or dark mode).
        /// </summary>
        public static string UserThemeChoice => Prefix + ".UserThemeChoice";

        /// <summary>
        /// Builds a fully qualified cookie name by prepending <see cref="Prefix"/> to <see cref="name"/>.
        /// </summary>
        public static string Build(string name) => Prefix + name;
    }
}