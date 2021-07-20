namespace Smartstore.Core.Theming
{
    /// <summary>
    /// Provides access to the current working theme.
    /// </summary>
    public partial interface IThemeContext
    {
        /// <summary>
        /// Gets or sets the name of the current theme (e.g. Flex)
        /// </summary>
        string WorkingThemeName { get; set; }

        /// <summary>
        /// Gets or sets the descriptor of the current working theme while respecting any request or session scoped override.
        /// </summary>
        ThemeDescriptor CurrentTheme { get; }

        /// <summary>
        /// Gets the theme override for the current request
        /// </summary>
        /// <returns>The theme override or <c>null</c></returns>
        string GetRequestTheme();

        /// <summary>
        /// Gets the theme override for the current session
        /// </summary>
        /// <returns>The theme override or <c>null</c></returns>
        string GetPreviewTheme();

        /// <summary>
        /// Sets a theme override to be used for the current request
        /// </summary>
        /// <param name="theme">The theme override or <c>null</c> to remove the override</param>
        void SetRequestTheme(string theme);

        /// <summary>
        /// Sets a theme override to be used for the current user's session (e.g. for preview mode)
        /// </summary>
        /// <param name="theme">The theme override or <c>null</c> to remove the override</param>
        void SetPreviewTheme(string theme);
    }
}