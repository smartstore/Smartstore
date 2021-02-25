namespace Smartstore.Web.Theming
{
    /// <summary>
    /// Responsible for resolving info about inherited theme file assets.
    /// </summary>
    public partial interface IThemeFileResolver
    {
        /// <summary>
        /// Tries to resolve a file up in the current theme's hierarchy chain.
        /// </summary>
        /// <param name="path">The original sub path of the theme file</param>
        /// <returns>
        /// If the current working themme is based on another theme AND the requested file
        /// was physically found in the theme's hierarchy chain, an instance of <see cref="InheritedThemeFileResult" /> will be returned.
        /// In any other case the return value is <c>null</c>.
        /// </returns>
        InheritedThemeFileResult Resolve(string path);
    }
}
