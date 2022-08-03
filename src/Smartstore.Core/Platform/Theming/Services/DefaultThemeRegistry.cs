using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Smartstore.Collections;
using Smartstore.Events;
using Smartstore.IO;
using Smartstore.Threading;

namespace Smartstore.Core.Theming
{
    public partial class DefaultThemeRegistry : Disposable, IThemeRegistry
    {
        private readonly IEventPublisher _eventPublisher;
        private readonly IApplicationContext _appContext;
        private readonly IFileSystem _root;
        private readonly ConcurrentDictionary<string, ThemeDescriptor> _themes = new(StringComparer.InvariantCultureIgnoreCase);
        private readonly ConcurrentDictionary<EventThrottleKey, Timer> _eventQueue = new();
        private readonly bool _enableMonitoring;

        private readonly Regex _fileFilterPattern = new(@"^\.(config|cshtml|scss|liquid)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private FileSystemWatcher _monitorFolders;
        private FileSystemWatcher _monitorFiles;

        public DefaultThemeRegistry(IEventPublisher eventPublisher, IApplicationContext appContext, bool? enableMonitoring, bool autoLoadThemes)
        {
            _enableMonitoring = enableMonitoring ?? appContext.AppConfiguration.MonitorThemesFolder;
            _eventPublisher = eventPublisher;
            _appContext = appContext;
            _root = appContext.ThemesRoot;

            if (autoLoadThemes)
            {
                // load all themes initially
                ReloadThemes();
            }

            CreateFileSystemWatchers();

            // start FS watcher
            StartMonitoring(false);
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        #region IThemeRegistry

        public event EventHandler<ThemeFileChangedEventArgs> ThemeFileChanged;
        public event EventHandler<ThemeDirectoryRenamedEventArgs> ThemeDirectoryRenamed;
        public event EventHandler<ThemeDirectoryDeletedEventArgs> ThemeDirectoryDeleted;
        public event EventHandler<BaseThemeChangedEventArgs> BaseThemeChanged;

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

                IFileSystem contentRoot = descriptor.IsSymbolicLink
                    ? new LocalFileSystem(descriptor.PhysicalPath)
                    : new ExpandedFileSystem(descriptor.Name, _appContext.ThemesRoot);

                if (baseDescriptor != null)
                {
                    // INFO: (core) Falling back to base theme's file via "?base" is not supported anymore.
                    // The full path must be specified instead, e.g. "/themes/flex/_variables.scss".
                    contentRoot = new CompositeFileSystem(contentRoot, baseDescriptor.ContentRoot);
                }

                descriptor.ContentRoot = contentRoot;
            }
        }

        private bool TryRemoveDescriptor(string themeName)
        {
            bool result;

            if (result = _themes.TryRemove(themeName, out var existing))
            {
                _eventPublisher.Publish(new ThemeTouchedEvent(themeName));

                existing.BaseTheme = null;

                // set all direct children as broken
                var children = GetChildrenOf(themeName, false);
                foreach (var child in children)
                {
                    child.BaseTheme = null;
                    child.State = ThemeDescriptorState.MissingBaseTheme;
                    _eventPublisher.Publish(new ThemeTouchedEvent(child.Name));
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
            _themes.Clear();

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
                    Logger.Error(ex, "Unable to create descriptor for theme '{0}'".FormatCurrent(dirData.Directory.Name));
                }
            }
        }

        #endregion

        #region Monitoring & Events

        private void CreateFileSystemWatchers()
        {
            _monitorFiles = new FileSystemWatcher
            {
                Path = _root.Root,
                InternalBufferSize = 32768, // // 32 instead of the default 8 KB,
                Filter = "*.*",
                NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.FileName,
                IncludeSubdirectories = true
            };

            _monitorFiles.Changed += (s, e) => OnThemeFileChanged(e.Name, e.FullPath, ThemeFileChangeType.Modified);
            _monitorFiles.Deleted += (s, e) => OnThemeFileChanged(e.Name, e.FullPath, ThemeFileChangeType.Deleted);
            _monitorFiles.Created += (s, e) => OnThemeFileChanged(e.Name, e.FullPath, ThemeFileChangeType.Created);
            _monitorFiles.Renamed += (s, e) =>
            {
                OnThemeFileChanged(e.OldName, e.OldFullPath, ThemeFileChangeType.Deleted);
                OnThemeFileChanged(e.Name, e.FullPath, ThemeFileChangeType.Created);
            };

            _monitorFolders = new FileSystemWatcher
            {
                Path = _root.Root,
                Filter = "*",
                NotifyFilter = NotifyFilters.DirectoryName,
                IncludeSubdirectories = false
            };

            _monitorFolders.Renamed += (s, e) => OnThemeFolderRenamed(e.Name, e.FullPath, e.OldName, e.OldFullPath);
            _monitorFolders.Deleted += (s, e) => OnThemeFolderDeleted(e.Name, e.FullPath);
        }

        public void StartMonitoring(bool force)
        {
            var shouldStart = force || _enableMonitoring;

            if (shouldStart && !_monitorFiles.EnableRaisingEvents)
                _monitorFiles.EnableRaisingEvents = true;
            if (shouldStart && !_monitorFolders.EnableRaisingEvents)
                _monitorFolders.EnableRaisingEvents = true;
        }

        public void StopMonitoring()
        {
            if (_monitorFiles.EnableRaisingEvents)
                _monitorFiles.EnableRaisingEvents = false;
            if (_monitorFolders.EnableRaisingEvents)
                _monitorFolders.EnableRaisingEvents = false;
        }

        private bool ShouldThrottleEvent(EventThrottleKey key)
        {
            if (_eventQueue.TryGetValue(key, out var timer))
            {
                // do nothing. The same event was published a tick ago.
                return true;
            }

            _eventQueue[key] = new Timer(RemoveFromEventQueue, key, 500, Timeout.Infinite);
            return false;
        }

        private void RemoveFromEventQueue(object key)
        {
            if (_eventQueue.TryRemove((EventThrottleKey)key, out var timer))
            {
                timer.Dispose();
            }
        }

        private void OnThemeFileChanged(string name, string fullPath, ThemeFileChangeType changeType)
        {
            ContextState.StartAsyncFlow();

            // Enable event throttling by allowing the very same event to be published only all 500 ms.
            var throttleKey = new EventThrottleKey(name, changeType);
            if (ShouldThrottleEvent(throttleKey))
            {
                return;
            }

            var ext = Path.GetExtension(name);
            if (!_fileFilterPattern.IsMatch(ext))
            {
                return;
            }

            var idx = name.IndexOf('\\');
            if (idx < 0)
            {
                // must be a subfolder of "/Themes/"
                return;
            }

            var themeName = name.Substring(0, idx);
            var relativePath = name[(themeName.Length + 1)..].Replace('\\', '/');
            var isConfigFile = relativePath.EqualsNoCase("theme.config");

            if (changeType == ThemeFileChangeType.Modified && !isConfigFile)
            {
                // Monitor changes only for root theme.config
                return;
            }

            BaseThemeChangedEventArgs baseThemeChangedArgs = null;

            var currentDescriptor = GetThemeDescriptor(themeName);

            if (!isConfigFile)
            {
                if (changeType == ThemeFileChangeType.Created)
                {
                    // If a file is being added to a derived theme's directory, any base file
                    // needs to be refreshed/cancelled. This is necessary, because the new file 
                    // overwrites the base file now, and RazorViewEngine/SassParser must be notified
                    // about this change.
                    var baseFile = currentDescriptor?.BaseTheme?.ContentRoot?.GetFile(relativePath);
                    if (baseFile != null && baseFile.Exists && baseFile is LocalFile localFile)
                    {
                        File.SetLastWriteTimeUtc(baseFile.PhysicalPath, DateTime.UtcNow);
                    }
                }
            }
            else
            {
                // Config file changes always result in refreshing the corresponding theme descriptor
                //var dir = new DirectoryInfo(Path.GetDirectoryName(fullPath));
                var dir = new LocalDirectory(themeName, new DirectoryInfo(Path.GetDirectoryName(fullPath)), _root as LocalFileSystem);

                string oldBaseThemeName = null;

                if (currentDescriptor != null)
                {
                    oldBaseThemeName = currentDescriptor.BaseThemeName;
                }

                try
                {
                    // FS watcher in conjunction with some text editors fires change events twice and locks the file.
                    // Let's wait max. 250ms till the lock is gone (hopefully).
                    var fi = new FileInfo(fullPath);
                    fi.WaitForUnlock(250);

                    var newDescriptor = ThemeDescriptor.Create(dir.Name, _root);
                    if (newDescriptor != null)
                    {
                        AddThemeDescriptorInternal(newDescriptor, false);

                        if (!oldBaseThemeName.EqualsNoCase(newDescriptor.BaseThemeName))
                        {
                            baseThemeChangedArgs = new BaseThemeChangedEventArgs
                            {
                                ThemeName = newDescriptor.Name,
                                BaseTheme = newDescriptor.BaseTheme?.Name,
                                OldBaseTheme = oldBaseThemeName
                            };
                        }

                        Logger.Debug("Changed theme descriptor for '{0}'".FormatCurrent(name));
                    }
                    else
                    {
                        // something went wrong (most probably no 'theme.config'): remove the descriptor
                        TryRemoveDescriptor(dir.Name);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Could not touch theme descriptor '{0}': {1}".FormatCurrent(name, ex.Message));
                    TryRemoveDescriptor(dir.Name);
                }
            }

            if (baseThemeChangedArgs != null)
            {
                BaseThemeChanged?.Invoke(this, baseThemeChangedArgs);
            }

            ThemeFileChanged?.Invoke(this, new ThemeFileChangedEventArgs
            {
                ChangeType = changeType,
                FullPath = fullPath,
                ThemeName = themeName,
                RelativePath = relativePath,
                IsConfigurationFile = isConfigFile
            });
        }

        private void OnThemeFolderRenamed(string name, string fullPath, string oldName, string oldFullPath)
        {
            ContextState.StartAsyncFlow();

            TryRemoveDescriptor(oldName);

            try
            {
                var newDescriptor = GetThemeDescriptor(name);
                if (newDescriptor != null)
                {
                    AddThemeDescriptorInternal(newDescriptor, false);
                    Logger.Debug("Changed theme descriptor for '{0}'".FormatCurrent(name));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Could not touch theme descriptor '{0}'".FormatCurrent(name));
            }

            ThemeDirectoryRenamed?.Invoke(this, new ThemeDirectoryRenamedEventArgs
            {
                FullPath = fullPath,
                Name = name,
                OldFullPath = oldFullPath,
                OldName = oldName
            });
        }

        private void OnThemeFolderDeleted(string name, string fullPath)
        {
            ContextState.StartAsyncFlow();

            TryRemoveDescriptor(name);

            ThemeDirectoryDeleted?.Invoke(this, new ThemeDirectoryDeletedEventArgs
            {
                FullPath = fullPath,
                Name = name
            });
        }

        #endregion

        #region Disposable

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                if (_monitorFiles != null)
                {
                    _monitorFiles.EnableRaisingEvents = false;
                    _monitorFiles.Dispose();
                    _monitorFiles = null;
                }

                if (_monitorFolders != null)
                {
                    _monitorFolders.EnableRaisingEvents = false;
                    _monitorFolders.Dispose();
                    _monitorFolders = null;
                }
            }
        }

        #endregion

        private class EventThrottleKey : Tuple<string, ThemeFileChangeType>
        {
            public EventThrottleKey(string name, ThemeFileChangeType changeType)
                : base(name, changeType)
            {
            }
        }
    }
}
