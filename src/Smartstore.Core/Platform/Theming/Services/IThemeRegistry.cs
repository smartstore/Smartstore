namespace Smartstore.Core.Theming
{
    public enum ThemeFileChangeType
    {
        Created,
        Deleted,
        Modified
    }

    public class ThemeFileChangedEventArgs : EventArgs
    {
        public string FullPath { get; set; }
        public string ThemeName { get; set; }
        public string RelativePath { get; set; }
        public bool IsConfigurationFile { get; set; }
        public ThemeFileChangeType ChangeType { get; set; }
    }

    public class ThemeDirectoryRenamedEventArgs : EventArgs
    {
        public string FullPath { get; set; }
        public string Name { get; set; }
        public string OldFullPath { get; set; }
        public string OldName { get; set; }
    }

    public class ThemeDirectoryDeletedEventArgs : EventArgs
    {
        public string FullPath { get; set; }
        public string Name { get; set; }
    }

    public class BaseThemeChangedEventArgs : EventArgs
    {
        public string ThemeName { get; set; }
        public string BaseTheme { get; set; }
        public string OldBaseTheme { get; set; }
    }

    /// <summary>
    /// Manages themes
    /// </summary>
    public partial interface IThemeRegistry
    {
        /// <summary>
        /// Gets the descriptors of all registered themes.
        /// </summary>
        /// <param name="includeHidden">Specifies whether inactive themes should also be included in the result list</param>
        /// <returns>A collection of <see cref="ThemeDescriptor"/> instances.</returns>
        ICollection<ThemeDescriptor> GetThemeDescriptors(bool includeHidden = false);

        /// <summary>
        /// Gets a single theme descriptor by theme name
        /// </summary>
        /// <param name="themeName">The name of the theme to get a descriptor for</param>
        /// <returns>A <see cref="ThemeDescriptor"/> instance or <c>null</c> if the theme is not registered.</returns>
        ThemeDescriptor GetThemeDescriptor(string themeName);

        /// <summary>
        /// Gets a value indicating whether a theme is registered
        /// </summary>
        /// <param name="themeName">The theme name to check</param>
        /// <returns><c>true</c> if theme exists, <c>false</c> otherwise</returns>
        bool ContainsTheme(string themeName);

        /// <summary>
        /// Registers a theme descriptor.
        /// </summary>
        /// <param name="descriptor">The theme descriptor to register</param>
        /// <remarks>If an equal theme exists already, it gets removed first.</remarks>
        void AddThemeDescriptor(ThemeDescriptor descriptor);

        /// <summary>
        /// Gets a value indicating whether a theme is a child of another theme
        /// </summary>
        /// <param name="themeName">The name of the theme to test</param>
        /// <param name="baseTheme">The name of the base theme</param>
        /// <returns><c>true</c> when <paramref name="themeName"/> is based on <paramref name="baseTheme"/>, <c>false</c> othwerise</returns>
        /// <remarks>
        /// This method walks up the complete hierarchy chain of <paramref name="themeName"/> to determine the result.
        /// </remarks>
        bool IsChildThemeOf(string themeName, string baseTheme);

        /// <summary>
        /// Gets all derived child themes 
        /// </summary>
        /// <param name="themeName">The name of the theme to get the children for</param>
        /// <param name="deep">When <c>true</c>, the method gets all child themes in the hierarchy chain, otherwise it only returns direct children.</param>
        /// <returns>The descriptors of matching themes</returns>
        IEnumerable<ThemeDescriptor> GetChildrenOf(string themeName, bool deep = true);

        /// <summary>
        /// Starts/resumes raising file system events
        /// </summary>
        /// <param name="force">
        /// When <c>true</c>, monitoring gets started regardless of the global setting (appsettings.json > Smartstore > MonitorThemesFolder),
        /// otherwise this method does nothing when the setting is <c>false</c>.
        /// </param>
        void StartMonitoring(bool force);

        /// <summary>
        /// Stops/pauses raising file system events
        /// </summary>
        void StopMonitoring();

        /// <summary>
        /// Clears all parsed theme descriptors and reloads them.
        /// </summary>
        void ReloadThemes();

        /// <summary>
        /// Event raised when an inheritable (static) theme file has been created or deleted,
        /// OR when the <c>theme.config</c> file has been modified.
        /// </summary>
        event EventHandler<ThemeFileChangedEventArgs> ThemeFileChanged;

        /// <summary>
        /// Event raised when a theme directory has been renamed.
        /// </summary>
        event EventHandler<ThemeDirectoryRenamedEventArgs> ThemeDirectoryRenamed;

        /// <summary>
        /// Event raised when a theme directory has been deleted.
        /// </summary>
        event EventHandler<ThemeDirectoryDeletedEventArgs> ThemeDirectoryDeleted;

        /// <summary>
        /// Event raised when a theme's base theme changes.
        /// </summary>
        event EventHandler<BaseThemeChangedEventArgs> BaseThemeChanged;
    }
}