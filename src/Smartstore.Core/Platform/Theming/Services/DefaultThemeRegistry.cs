using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Smartstore.Collections;
using Smartstore.IO;
using Smartstore.Threading;

namespace Smartstore.Core.Theming
{
    public partial class DefaultThemeRegistry : Disposable, IThemeRegistry
    {
        private readonly IApplicationContext _appContext;
        private readonly IMemoryCache _memCache;
        private readonly IFileSystem _root;
        private readonly ConcurrentDictionary<string, ThemeDescriptor> _themes = new(StringComparer.InvariantCultureIgnoreCase);
        private readonly FileSystemWatcher _directoryWatcher;

        public DefaultThemeRegistry(IApplicationContext appContext, IMemoryCache memCache, bool autoLoadThemes)
        {
            _appContext = appContext;
            _memCache = memCache;
            _root = appContext.ThemesRoot;

            if (autoLoadThemes)
            {
                // load all themes initially
                ReloadThemes();
            }

            _directoryWatcher = new FileSystemWatcher
            {
                Path = _root.Root,
                Filter = "*",
                NotifyFilter = NotifyFilters.DirectoryName,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };

            _directoryWatcher.Deleted += (s, e) => OnThemeFolderDeleted(e.Name);
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        #region IThemeRegistry

        public event EventHandler<ThemeExpiredEventArgs> ThemeExpired;

        public bool ContainsTheme(string themeName)
        {
            if (themeName.IsEmpty())
                return false;

            if (_themes.TryGetValue(themeName, out var descriptor))
            {
                return descriptor.State == ThemeDescriptorState.Active;
            }

            return false;
        }

        public ThemeDescriptor GetThemeDescriptor(string themeName)
        {
            if (themeName.HasValue() && _themes.TryGetValue(themeName, out var descriptor))
            {
                return descriptor;
            }

            return null;
        }

        public ICollection<ThemeDescriptor> GetThemeDescriptors(bool includeHidden = false)
        {
            var allThemes = _themes.Values;

            if (includeHidden)
            {
                return allThemes.AsReadOnly();
            }
            else
            {
                return allThemes.Where(x => x.State == ThemeDescriptorState.Active).AsReadOnly();
            }
        }

        public void AddThemeDescriptor(ThemeDescriptor descriptor)
        {
            AddThemeDescriptorInternal(descriptor, false);
        }

        private void AddThemeDescriptorInternal(ThemeDescriptor descriptor, bool isInit)
        {
            Guard.NotNull(descriptor, nameof(descriptor));

            if (!isInit)
            {
                TryRemoveDescriptor(descriptor.Name);
            }

            ThemeDescriptor baseDescriptor = null;
            if (descriptor.BaseThemeName != null)
            {
                if (!_themes.TryGetValue(descriptor.BaseThemeName, out baseDescriptor))
                {
                    descriptor.State = ThemeDescriptorState.MissingBaseTheme;
                }
            }
            
            descriptor.BaseTheme = baseDescriptor;
            var added = _themes.TryAdd(descriptor.Name, descriptor);
            if (added)
            {
                // Post process
                if (!isInit)
                {
                    var children = GetChildrenOf(descriptor.Name, false);
                    foreach (var child in children)
                    {
                        child.BaseTheme = descriptor;
                        child.State = ThemeDescriptorState.Active;
                    }
                }

                IFileSystem contentRoot = null;

                if (descriptor.CompanionModuleName.HasValue())
                {
                    // If a theme has a companion module, the content root is actually the module's content root.
                    // Because such themes are always symlinked to the module directory.
                    var module = _appContext.ModuleCatalog.GetModuleByName(descriptor.CompanionModuleName);
                    descriptor.CompanionModule = module;
                    contentRoot = module?.ContentRoot;
                }

                contentRoot ??= descriptor.IsSymbolicLink
                    ? new LocalFileSystem(descriptor.PhysicalPath)
                    : new ExpandedFileSystem(descriptor.Name, _appContext.ThemesRoot);

                // INFO: (core) Falling back to base theme's file via "?base" is not supported anymore.
                // The full path must be specified instead, e.g. "/themes/flex/_variables.scss".
                descriptor.ContentRoot = new ThemeFileSystem(contentRoot, baseDescriptor?.ContentRoot);

                // Rebase configuration file to ContentRoot
                descriptor.ConfigurationFile = contentRoot.GetFile(descriptor.ConfigurationFile.Name);

                // Register "theme.config" expiration token
                var configWatcher = descriptor.ContentRoot
                    .Watch("theme.config")
                    .RegisterChangeCallback(OnConfigurationChanged, descriptor);

                // To dispose registrations together with the descriptor.
                descriptor.FileWatchers = new[] { configWatcher };
            }
        }

        private bool TryRemoveDescriptor(string themeName)
        {
            bool result;

            if (result = _themes.TryRemove(themeName, out var existing))
            {
                ContextState.StartAsyncFlow();

                ThemeExpired?.Invoke(this, new ThemeExpiredEventArgs { ThemeName = themeName, Cache = _memCache });

                existing.Dispose();

                // Set all direct children as broken
                var children = GetChildrenOf(themeName, false);
                foreach (var child in children)
                {
                    child.BaseTheme = null;
                    child.State = ThemeDescriptorState.MissingBaseTheme;

                    ThemeExpired?.Invoke(this, new ThemeExpiredEventArgs { ThemeName = child.Name, Cache = _memCache });
                }
            }

            return result;
        }

        public bool IsChildThemeOf(string themeName, string baseTheme)
        {
            if (themeName.IsEmpty() && baseTheme.IsEmpty())
            {
                return false;
            }

            if (themeName.Equals(baseTheme, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var current = GetThemeDescriptor(themeName);
            if (current == null)
                return false;

            while (current.BaseThemeName != null)
            {
                if (baseTheme.Equals(current.BaseThemeName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (!_themes.TryGetValue(current.BaseThemeName, out current))
                {
                    return false;
                }
                //currentBaseName = current.BaseThemeName;
            }

            return false;
        }

        public IEnumerable<ThemeDescriptor> GetChildrenOf(string themeName, bool deep = true)
        {
            Guard.NotEmpty(themeName, nameof(themeName));

            if (!ContainsTheme(themeName))
                return Enumerable.Empty<ThemeDescriptor>();

            var derivedThemes = _themes.Values.Where(x => x.BaseThemeName != null && !x.Name.EqualsNoCase(themeName));
            if (!deep)
            {
                derivedThemes = derivedThemes.Where(x => x.BaseThemeName.EqualsNoCase(themeName));
            }
            else
            {
                derivedThemes = derivedThemes.Where(x => IsChildThemeOf(x.Name, themeName));
            }

            return derivedThemes;
        }

        public void ReloadThemes()
        {
            ClearThemes();

            var dirDatas = new List<ThemeDirectoryData>();
            var dirs = _root.EnumerateDirectories("");

            // Create folder (meta)datas first
            foreach (var dir in dirs)
            {
                try
                {
                    var dirData = ThemeDescriptor.CreateThemeDirectoryData(dir, _root);
                    if (dirData != null)
                    {
                        dirDatas.Add(dirData);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Unable to collect theme data for directory '{0}'".FormatCurrent(dir.SubPath));
                }
            }

            // Perform topological sort (BaseThemes first...)
            IEnumerable<ThemeDirectoryData> sortedThemeDirs;
            try
            {
                sortedThemeDirs = dirDatas.ToArray()
                    .SortTopological(StringComparer.OrdinalIgnoreCase)
                    .Cast<ThemeDirectoryData>();
            }
            catch (CyclicDependencyException)
            {
                var ex = new CyclicDependencyException("Cyclic theme dependencies detected. Please check the 'baseTheme' attribute of your themes and ensure that they do not reference each other (in)directly.");
                Logger.Error(ex);
                throw ex;
            }
            catch
            {
                throw;
            }

            // Create theme descriptor
            foreach (var dirData in sortedThemeDirs)
            {
                try
                {
                    var descriptor = ThemeDescriptor.Create(dirData);
                    if (descriptor != null)
                    {
                        AddThemeDescriptorInternal(descriptor, true);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Unable to create descriptor for theme '{0}'".FormatCurrent(dirData.Name));
                }
            }
        }

        private void ClearThemes()
        {
            foreach (var descriptor in _themes.Values)
            {
                descriptor.Dispose();
            }

            _themes.Clear();
        }

        #endregion

        #region Monitoring & Events

        private void OnConfigurationChanged(object state)
        {
            // Config file changes always result in refreshing the corresponding theme descriptor,
            // and also all child theme descriptors.

            var descriptor = (ThemeDescriptor)state;

            try
            {
                var newDescriptor = ThemeDescriptor.Create(descriptor.Name, _root);
                if (newDescriptor != null)
                {
                    AddThemeDescriptorInternal(newDescriptor, false);
                    Logger.Debug("Changed theme descriptor for '{0}'".FormatCurrent(descriptor.Name));
                }
                else
                {
                    // Something went wrong (most probably no 'theme.config'): remove the descriptor
                    TryRemoveDescriptor(descriptor.Name);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Could not touch theme descriptor '{0}': {1}".FormatCurrent(descriptor.Name, ex.Message));
                TryRemoveDescriptor(descriptor.Name);
            }
        }

        private void OnThemeFolderDeleted(string name)
        {
            TryRemoveDescriptor(name);
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                ClearThemes();

                if (_directoryWatcher != null)
                {
                    _directoryWatcher.EnableRaisingEvents = false;
                    _directoryWatcher.Dispose();
                }
            }
        }

        #endregion
    }
}
