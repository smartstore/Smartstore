using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Collections;
using Smartstore.Engine;
using Smartstore.Events;
using Smartstore.IO;

namespace Smartstore.Web.Common.Theming
{
    public partial class DefaultThemeRegistry : Disposable, IThemeRegistry
    {
        private readonly IEventPublisher _eventPublisher;
        private readonly IApplicationContext _appContext;
        private readonly IFileSystem _root;
        private readonly ConcurrentDictionary<string, ThemeManifest> _themes = new ConcurrentDictionary<string, ThemeManifest>(StringComparer.InvariantCultureIgnoreCase);
        private readonly ConcurrentDictionary<EventThrottleKey, Timer> _eventQueue = new ConcurrentDictionary<EventThrottleKey, Timer>();
        private readonly bool _enableMonitoring;

        private readonly Regex _fileFilterPattern = new Regex(@"^\.(json|png|gif|jpg|jpeg|css|scss|js|cshtml|svg)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
            this.StartMonitoring(false);
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        #region IThemeRegistry

        public event EventHandler<ThemeFileChangedEventArgs> ThemeFileChanged;
        public event EventHandler<ThemeFolderRenamedEventArgs> ThemeFolderRenamed;
        public event EventHandler<ThemeFolderDeletedEventArgs> ThemeFolderDeleted;
        public event EventHandler<BaseThemeChangedEventArgs> BaseThemeChanged;

        public bool ThemeManifestExists(string themeName)
        {
            if (themeName.IsEmpty())
                return false;

            if (_themes.TryGetValue(themeName, out var manifest))
            {
                return manifest.State == ThemeManifestState.Active;
            }

            return false;
        }

        public ThemeManifest GetThemeManifest(string themeName)
        {
            if (themeName.HasValue() && _themes.TryGetValue(themeName, out var manifest))
            {
                return manifest;
            }

            return null;
        }

        public ICollection<ThemeManifest> GetThemeManifests(bool includeHidden = false)
        {
            var allThemes = _themes.Values;

            if (includeHidden)
            {
                return allThemes.AsReadOnly();
            }
            else
            {
                return allThemes.Where(x => x.State == ThemeManifestState.Active).AsReadOnly();
            }
        }

        public void AddThemeManifest(ThemeManifest manifest)
        {
            AddThemeManifestInternal(manifest, false);
        }

        private void AddThemeManifestInternal(ThemeManifest manifest, bool isInit)
        {
            Guard.NotNull(manifest, nameof(manifest));

            if (!isInit)
            {
                TryRemoveManifest(manifest.ThemeName);
            }

            ThemeManifest baseManifest = null;
            if (manifest.BaseThemeName != null)
            {
                if (!_themes.TryGetValue(manifest.BaseThemeName, out baseManifest))
                {
                    manifest.State = ThemeManifestState.MissingBaseTheme;
                }
            }

            manifest.BaseTheme = baseManifest;
            var added = _themes.TryAdd(manifest.ThemeName, manifest);
            if (added && !isInit)
            {
                // post process
                var children = GetChildrenOf(manifest.ThemeName, false);
                foreach (var child in children)
                {
                    child.BaseTheme = manifest;
                    child.State = ThemeManifestState.Active;
                }
            }
        }

        private bool TryRemoveManifest(string themeName)
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
                    child.State = ThemeManifestState.MissingBaseTheme;
                    _eventPublisher.Publish(new ThemeTouchedEvent(child.ThemeName));
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

            var current = GetThemeManifest(themeName);
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

        public IEnumerable<ThemeManifest> GetChildrenOf(string themeName, bool deep = true)
        {
            Guard.NotEmpty(themeName, nameof(themeName));

            if (!ThemeManifestExists(themeName))
                return Enumerable.Empty<ThemeManifest>();

            var derivedThemes = _themes.Values.Where(x => x.BaseThemeName != null && !x.ThemeName.IsCaseInsensitiveEqual(themeName));
            if (!deep)
            {
                derivedThemes = derivedThemes.Where(x => x.BaseThemeName.IsCaseInsensitiveEqual(themeName));
            }
            else
            {
                derivedThemes = derivedThemes.Where(x => IsChildThemeOf(x.ThemeName, themeName));
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
                    var dirData = ThemeManifest.CreateThemeDirectoryData(dir, _root);
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

            // Create theme manifests
            foreach (var dirData in sortedThemeDirs)
            {
                try
                {
                    var manifest = ThemeManifest.Create(dirData);
                    if (manifest != null)
                    {
                        AddThemeManifestInternal(manifest, true);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Unable to create manifest for theme '{0}'".FormatCurrent(dirData.Directory.Name));
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
            // Enable event throttling by allowing the very same event to be published only all 500 ms.
            var throttleKey = new EventThrottleKey(name, changeType);
            if (ShouldThrottleEvent(throttleKey))
            {
                return;
            }

            if (!_fileFilterPattern.IsMatch(Path.GetExtension(name)))
                return;

            var idx = name.IndexOf('\\');
            if (idx < 0)
            {
                // must be a subfolder of "/Themes/"
                return;
            }

            var themeName = name.Substring(0, idx);
            var relativePath = name[(themeName.Length + 1)..].Replace('\\', '/');
            var isConfigFile = relativePath.IsCaseInsensitiveEqual("theme.json");

            if (changeType == ThemeFileChangeType.Modified && !isConfigFile)
            {
                // Monitor changes only for root theme.json
                return;
            }

            BaseThemeChangedEventArgs baseThemeChangedArgs = null;

            if (isConfigFile)
            {
                // config file changes always result in refreshing the corresponding theme manifest
                //var dir = new DirectoryInfo(Path.GetDirectoryName(fullPath));
                var dir = new LocalDirectory(themeName, new DirectoryInfo(Path.GetDirectoryName(fullPath)));

                string oldBaseThemeName = null;
                var oldManifest = GetThemeManifest(dir.Name);
                if (oldManifest != null)
                {
                    oldBaseThemeName = oldManifest.BaseThemeName;
                }

                try
                {
                    // FS watcher in conjunction with some text editors fires change events twice and locks the file.
                    // Let's wait max. 250ms till the lock is gone (hopefully).
                    var fi = new FileInfo(fullPath);
                    fi.WaitForUnlock(250);

                    var newManifest = ThemeManifest.Create(dir.Name, _root);
                    if (newManifest != null)
                    {
                        AddThemeManifestInternal(newManifest, false);

                        if (!oldBaseThemeName.IsCaseInsensitiveEqual(newManifest.BaseThemeName))
                        {
                            baseThemeChangedArgs = new BaseThemeChangedEventArgs
                            {
                                ThemeName = newManifest.ThemeName,
                                BaseTheme = newManifest.BaseTheme?.ThemeName,
                                OldBaseTheme = oldBaseThemeName
                            };
                        }

                        Logger.Debug("Changed theme manifest for '{0}'".FormatCurrent(name));
                    }
                    else
                    {
                        // something went wrong (most probably no 'theme.config'): remove the manifest
                        TryRemoveManifest(dir.Name);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Could not touch theme manifest '{0}': {1}".FormatCurrent(name, ex.Message));
                    TryRemoveManifest(dir.Name);
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
            TryRemoveManifest(oldName);

            try
            {
                var newManifest = GetThemeManifest(name);
                if (newManifest != null)
                {
                    AddThemeManifestInternal(newManifest, false);
                    Logger.Debug("Changed theme manifest for '{0}'".FormatCurrent(name));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Could not touch theme manifest '{0}'".FormatCurrent(name));
            }

            ThemeFolderRenamed?.Invoke(this, new ThemeFolderRenamedEventArgs
            {
                FullPath = fullPath,
                Name = name,
                OldFullPath = oldFullPath,
                OldName = oldName
            });
        }

        private void OnThemeFolderDeleted(string name, string fullPath)
        {
            TryRemoveManifest(name);

            ThemeFolderDeleted?.Invoke(this, new ThemeFolderDeletedEventArgs
            {
                FullPath = fullPath,
                Name = name
            });
        }

        #endregion

        #region Disposable

        protected override void OnDispose(bool disposing)
        {
            //if (disposing)
            //{
            //    if (_monitorFiles != null)
            //    {
            //        _monitorFiles.EnableRaisingEvents = false;
            //        _monitorFiles.Dispose();
            //        _monitorFiles = null;
            //    }

            //    if (_monitorFolders != null)
            //    {
            //        _monitorFolders.EnableRaisingEvents = false;
            //        _monitorFolders.Dispose();
            //        _monitorFolders = null;
            //    }
            //}
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
