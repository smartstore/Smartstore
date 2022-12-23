using Smartstore.Events;
using Smartstore.IO;
using Smartstore.Threading;

namespace Smartstore.Core.Theming
{
    public class ThemeEventArgs : EventArgs
    {
        public string ThemeName { get; set; }
    }

    public class ThemeMonitor : Disposable
    {
        private readonly IFileSystem _root;
        private readonly IEventPublisher _eventPublisher;

        private DefaultThemeRegistry _registry;
        private FileSystemWatcher _directoryWatcher;
        private FileSystemWatcher _configWatcher;
        private FileSystemWatcher _sassWatcher;

        public ThemeMonitor(DefaultThemeRegistry registry, IFileSystem root, IEventPublisher eventPublisher)
        {
            _registry = Guard.NotNull(registry);
            _root = Guard.NotNull(root);
            _eventPublisher = Guard.NotNull(eventPublisher);
        }

        public event EventHandler<ThemeDirectoryRenamedEventArgs> Renamed;
        public event EventHandler<ThemeDirectoryDeletedEventArgs> Deleted;
        public event EventHandler<ThemeEventArgs> ConfigurationChanged;
        public event EventHandler<ThemeEventArgs> Expired;

        public void Start()
        {
            if (_directoryWatcher == null || _configWatcher == null || _sassWatcher == null) 
            {
                CreateWatchers();
            }

            _sassWatcher.EnableRaisingEvents = true;
            _configWatcher.EnableRaisingEvents = true;
            _directoryWatcher.EnableRaisingEvents = true;
        }

        public void Stop()
        {
            if (_directoryWatcher != null && _directoryWatcher.EnableRaisingEvents)
            {
                _directoryWatcher.EnableRaisingEvents = false;
            }

            if (_configWatcher != null && _configWatcher.EnableRaisingEvents)
            {
                _configWatcher.EnableRaisingEvents = false;
            }

            if (_sassWatcher != null && _sassWatcher.EnableRaisingEvents)
            {
                _sassWatcher.EnableRaisingEvents = false;
            }
        }

        private void CreateWatchers()
        {
            _sassWatcher = new FileSystemWatcher
            {
                Path = _root.Root,
                Filter = "*.scss",
                NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.FileName,
                IncludeSubdirectories = true
            };

            _sassWatcher.Created += OnSassEvent;

            _configWatcher = new FileSystemWatcher
            {
                Path = _root.Root,
                Filter = "theme.config",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                IncludeSubdirectories = true
            };

            _configWatcher.Changed += OnConfigEvent;
            // A change in VS results in Renamed raised twice
            _configWatcher.Renamed += OnConfigEvent;

            _directoryWatcher = new FileSystemWatcher
            {
                Path = _root.Root,
                Filter = "*",
                NotifyFilter = NotifyFilters.DirectoryName,
                IncludeSubdirectories = false
            };
        }

        private void OnConfigEvent(object sender, FileSystemEventArgs e)
        {
            if (ConfigurationChanged == null)
            {
                return;
            }
            
            if (!TokenizeName(e.Name, out var themeName, out var relativePath))
            {
                return;
            }

            if (e.ChangeType == WatcherChangeTypes.Renamed && relativePath != "theme.config")
            {
                // In VS this is the first of two consecutive events.
                return;
            }

            ContextState.StartAsyncFlow();
            ConfigurationChanged.Invoke(this, new ThemeEventArgs { ThemeName = themeName });
        }

        private void OnSassEvent(object sender, FileSystemEventArgs e)
        {
            // If a file is being added to a derived theme's directory, any base file
            // needs to be refreshed/cancelled. This is necessary, because the new file 
            // overwrites the base file now, and RazorViewEngine/SassParser must be notified
            // about this change.

            if (e.ChangeType != WatcherChangeTypes.Created && e.ChangeType != WatcherChangeTypes.Renamed)
            {
                return;
            }

            if (!TokenizeName(e.Name, out var themeName, out var relativePath))
            {
                return;
            }

            var currentDescriptor = _registry.GetThemeDescriptor(themeName);

            var baseFile = currentDescriptor?.BaseTheme?.ContentRoot?.GetFile(relativePath);
            if (baseFile != null && baseFile.Exists && baseFile is LocalFile)
            {
                File.SetLastWriteTimeUtc(baseFile.PhysicalPath, DateTime.UtcNow);
            }
        }

        private static bool TokenizeName(string name, out string themeName, out string relativePath)
        {
            themeName = null;
            relativePath = null;

            var idx = name.IndexOfAny(PathUtility.PathSeparators);
            if (idx < 0)
            {
                // must be a subfolder of "/Themes/" or "/Modules/"
                return false;
            }

            themeName = name[..idx];
            relativePath = name[(themeName.Length + 1)..].Replace('\\', '/');

            return true;
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                _registry = null;

                if (_directoryWatcher != null)
                {
                    _directoryWatcher.Dispose();
                    _directoryWatcher = null;
                }

                if (_configWatcher != null)
                {
                    _configWatcher.Dispose();
                    _configWatcher = null;
                }

                if (_sassWatcher != null)
                {
                    _sassWatcher.Dispose();
                    _sassWatcher = null;
                }
            }
        }
    }
}
