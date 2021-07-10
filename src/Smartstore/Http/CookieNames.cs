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
        /// Gets the session cookie name
        /// </summary>
        public static string Session => Prefix + ".Session";

        /// <summary>
        /// Gets the TempData cookie name
        /// </summary>
        public static string TempData => Prefix + ".TempData";

        /// <summary>
        /// Gets the identity (registered account) cookie name
        /// </summary>
        public static string Identity => Prefix + ".Identity";

        /// <summary>
        /// Gets the identity external authentication cookie name
        /// </summary>
        public static string ExternalAuthentication => Prefix + ".ExternalAuth";

        /// <summary>
        /// Gets the visitor (guest account) cookie name
        /// </summary>
        public static string Visitor => Prefix + ".Visitor";

        /// <summary>
        /// Gets the antiforgery cookie name
        /// </summary>
        public static string Antiforgery => Prefix + ".Antiforgery";

        /// <summary>
        /// Gets the compared products cookie name
        /// </summary>
        public static string ComparedProducts => Prefix + ".ComparedProducts";

        /// <summary>
        /// Gets the recently viewed products cookie name
        /// </summary>
        public static string RecentlyViewedProducts => Prefix + ".RecentlyViewedProducts";

        /// <summary>
        /// Gets the GdprConsent cookie name
        /// </summary>
        public static string GdprConsent => Prefix + ".Consent";

        /// <summary>
        /// Gets the cookie name of the cookie consent manager.
        /// </summary>
        public static string CookieConsent => Prefix + ".CookieConsent";

        /// <summary>
        /// Gets the StoreIdOverride cookie name
        /// </summary>
        public static string StoreIdOverride => Prefix + ".StoreIdOverride";

        /// <summary>
        /// Gets the PreviewModeOverride cookie name
        /// </summary>
        public static string PreviewModeOverride => Prefix + ".PreviewModeOverride";

        /// <summary>
        /// Gets the PreviewToolOpen cookie name
        /// </summary>
        public static string PreviewToolOpen => Prefix + ".PreviewToolOpen";

        /// <summary>
        /// Gets the UserThemeChoice cookie name
        /// </summary>
        public static string UserThemeChoice => Prefix + ".UserThemeChoice";

        /// <summary>
        /// Builds a fully qualified cookie name by prepending <see cref="Prefix"/> to <see cref="name"/>.
        /// </summary>
        public static string Build(string name) => Prefix + name;
    }
}