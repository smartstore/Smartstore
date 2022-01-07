namespace Smartstore.Core.Web
{
    /// <summary>
    /// Allows short-term overwriting of context data in a cookie, for preview purposes.
    /// </summary>
    public interface IPreviewModeCookie
    {
        /// <summary>
        /// Reads an override with the given <paramref name="key"/> from the cookie.
        /// </summary>
        /// <returns>The overriden value or <c>null</c>.</returns>
        string GetOverride(string key);

        /// <summary>
        /// Writes an override to the cookie on response start. The override takes effect during the next request.
        /// </summary>
        void SetOverride(string key, string value);

        /// <summary>
        /// Removes an override from the cookie.
        /// </summary>
        bool RemoveOverride(string key);

        /// <summary>
        /// Reads all existing override keys from the cookie.
        /// </summary>
        ICollection<string> AllOverrideKeys { get; }
    }
}
