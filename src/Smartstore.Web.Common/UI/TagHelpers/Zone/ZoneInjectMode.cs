namespace Smartstore.Web.UI.TagHelpers
{
    /// <summary>
    /// Content injection modes.
    /// </summary>
    public enum ZoneInjectMode
    {
        /// <summary>
        /// Appends injected content to existing content.
        /// </summary>
        Append,

        /// <summary>
        /// Prepends injected content to existing content.
        /// </summary>
        Prepend,

        /// <summary>
        /// Replaces existing with injected content.
        /// </summary>
        Replace
    }
}
