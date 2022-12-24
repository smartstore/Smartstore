using System.Xml;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Smartstore.Collections;
using Smartstore.Engine.Modularity;
using Smartstore.IO;

namespace Smartstore.Core.Theming
{
    public enum ThemeDescriptorState
    {
        MissingBaseTheme = -1,
        Active = 0
    }

    public class ThemeDescriptor : IExtensionDescriptor, IExtensionLocation, IDisposable
    {
        private IFileProvider _webFileProvider;

        internal ThemeDescriptor()
        {
        }

        #region Static

        public static ThemeDescriptor Create(string themeName, IFileSystem root)
        {
            Guard.NotEmpty(themeName, nameof(themeName));

            var directoryData = CreateThemeDirectoryData(root.GetDirectory(themeName), root);
            if (directoryData != null)
            {
                return Create(directoryData);
            }

            return null;
        }

        internal static ThemeDescriptor Create(ThemeDirectoryData directoryData)
        {
            Guard.NotNull(directoryData, nameof(directoryData));

            var materializer = new ThemeDescriptorMaterializer(directoryData);
            var descriptor = materializer.Materialize();

            return descriptor;
        }

        internal static ThemeDirectoryData CreateThemeDirectoryData(IDirectory themeDirectory, IFileSystem root)
        {
            if (!themeDirectory.Exists)
                return null;

            var themeName = themeDirectory.Name;
            var isSymLink = themeDirectory.IsSymbolicLink(out var linkedPath);
            if (isSymLink)
            {
                themeDirectory = new LocalDirectory(themeDirectory.SubPath, new DirectoryInfo(linkedPath), root as LocalFileSystem);
            }

            var themeConfigFile = root.GetFile(PathUtility.Join(themeName, "theme.config"));

            if (themeConfigFile.Exists)
            {
                var doc = new XmlDocument();
                using var stream = themeConfigFile.OpenRead();
                doc.Load(stream);

                Guard.Against<InvalidOperationException>(doc.DocumentElement == null, "The theme configuration document must have a root element.");

                var rootNode = doc.DocumentElement;

                var baseTheme = rootNode.GetAttribute("baseTheme").TrimSafe().NullEmpty();
                if (baseTheme != null && baseTheme.EqualsNoCase(themeName))
                {
                    // Don't let theme base on itself!
                    baseTheme = null;
                }

                return new ThemeDirectoryData
                {
                    Name = themeName,
                    Directory = themeDirectory,
                    ConfigurationFile = themeConfigFile,
                    ConfigurationNode = rootNode,
                    IsSymbolicLink = isSymLink,
                    BaseTheme = baseTheme
                };
            }

            return null;
        }

        #endregion

        #region File Monitoring

        private FileSystemWatcher _sassWatcher;
        private IChangeToken _expirationToken;
        private List<IDisposable> _expirationTokenRegistrations;

        internal void StartSassWatcher()
        {
            if (_sassWatcher == null)
            {
                lock (this)
                {
                   if (_sassWatcher == null)
                    {
                        _sassWatcher = new FileSystemWatcher
                        {
                            Path = ContentRoot.Root,
                            Filter = "*.scss",
                            NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.FileName,
                            IncludeSubdirectories = true
                        };

                        _sassWatcher.Created += OnSassEvent;
                        _sassWatcher.Renamed += OnSassEvent;
                    }
                }
            }

            _sassWatcher.EnableRaisingEvents = true;
        }

        internal void StopSassWatcher()
        {
            if (_sassWatcher != null)
            {
                _sassWatcher.EnableRaisingEvents = false;
            }
        }

        /// <summary>
        /// Gets the <see cref="IChangeToken"/> instance for the root <c>theme.config</c> file.
        /// </summary>
        internal IChangeToken ExpirationToken
        {
            get
            {
                if (_expirationToken == null)
                {
                    lock (this)
                    {
                        _expirationToken ??= ContentRoot.Watch("theme.config");
                    }
                }

                return _expirationToken;
            }
        }

        /// <summary>
        /// Registers a callback to invoke when the configuration file changes.
        /// Any registration will be disposed when the descriptor instance is disposed.
        /// </summary>ite
        /// <param name="callback">The callback. The action parameter is the <see cref="ThemeDescriptor"/> instance.</param>
        internal IDisposable RegisterExpirationCallback(Action<object> callback)
        {
            Guard.NotNull(callback);

            var token = ExpirationToken;
            if (token != null)
            {
                _expirationTokenRegistrations ??= new(1);

                var registration = token.RegisterChangeCallback(callback, this);
                _expirationTokenRegistrations.Add(registration);
                return registration;
            }

            return null;
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

            var baseFile = BaseTheme?.ContentRoot?.GetFile(e.Name);
            if (baseFile != null && baseFile.Exists && baseFile is LocalFile)
            {
                File.SetLastWriteTimeUtc(baseFile.PhysicalPath, DateTime.UtcNow);
            }
        }

        private void DetachFileWatchers()
        {
            if (_sassWatcher != null)
            {
                _sassWatcher.Dispose();
                _sassWatcher = null;
            }

            _expirationToken = null;
            if (_expirationTokenRegistrations != null)
            {
                foreach (var registration in _expirationTokenRegistrations)
                {
                    registration.Dispose();
                }

                _expirationTokenRegistrations.Clear();
                _expirationTokenRegistrations = null;
            }
        }

        #endregion

        #region IExtensionDescriptor

        /// <inheritdoc/>
        ExtensionType IExtensionDescriptor.ExtensionType
            => ExtensionType.Theme;

        /// <inheritdoc/>
        [ObjectSignature]
        public string Name { get; internal set; }

        /// <inheritdoc/>
        public string FriendlyName { get; internal set; }

        /// <inheritdoc/>
        public string Description { get; internal set; }

        /// <inheritdoc/>
        public string Group { get; internal set; }

        /// <inheritdoc/>
        public string Author { get; internal set; }

        /// <inheritdoc/>
        public string ProjectUrl { get; internal set; }

        /// <inheritdoc/>
        public string Tags { get; internal set; }

        /// <inheritdoc/>
        public Version Version { get; internal set; }

        /// <inheritdoc/>
        public Version MinAppVersion { get; internal set; }

        #endregion

        #region IExtensionLocation

        /// <inheritdoc/>
        public string Path { get; internal set; }

        /// <inheritdoc/>
        public string PhysicalPath { get; internal set; }

        /// <inheritdoc/>
        public IFileSystem ContentRoot { get; internal set; }

        /// <inheritdoc/>
        public IFileProvider WebRoot
        {
            get => _webFileProvider ??= new ExpandedFileSystem("wwwroot", ContentRoot);
            internal set => _webFileProvider = Guard.NotNull(value, nameof(value));
        }

        #endregion

        public IFile ConfigurationFile { get; internal set; }

        /// <summary>
        /// Determines whether the theme directory is a symbolic link to another target.
        /// </summary>
        public bool IsSymbolicLink { get; internal set; }

        public string PreviewImagePath { get; internal set; }

        public string BaseThemeName { get; internal set; }

        public ThemeDescriptor BaseTheme { get; internal set; }

        public string CompanionModuleName { get; internal set; }

        public IModuleDescriptor CompanionModule { get; internal set; }

        private IDictionary<string, ThemeVariableInfo> _variables;
        public IDictionary<string, ThemeVariableInfo> Variables
        {
            get
            {
                if (BaseTheme == null)
                {
                    return _variables;
                }

                var baseVars = BaseTheme.Variables;
                var mergedVars = new Dictionary<string, ThemeVariableInfo>(baseVars, StringComparer.OrdinalIgnoreCase);
                var newVars = new List<ThemeVariableInfo>();

                foreach (var localVar in _variables)
                {
                    if (mergedVars.ContainsKey(localVar.Key))
                    {
                        // Overridden var in child: update existing.
                        var baseVar = mergedVars[localVar.Key];
                        mergedVars[localVar.Key] = new ThemeVariableInfo
                        {
                            Name = baseVar.Name,
                            Type = baseVar.Type,
                            SelectRef = baseVar.SelectRef,
                            DefaultValue = localVar.Value.DefaultValue,
                            ThemeDescriptor = localVar.Value.ThemeDescriptor
                        };
                    }
                    else
                    {
                        // New var in child: add to temp list.
                        newVars.Add(localVar.Value);
                    }
                }

                var merged = new Dictionary<string, ThemeVariableInfo>(StringComparer.OrdinalIgnoreCase);

                foreach (var newVar in newVars)
                {
                    // New child theme vars must come first in final list
                    // to avoid wrong references in existing vars
                    merged.Add(newVar.Name, newVar);
                }

                foreach (var kvp in mergedVars)
                {
                    merged.Add(kvp.Key, kvp.Value);
                }

                return merged;
            }
            internal set => _variables = value;
        }

        private Multimap<string, string> _selects;
        public Multimap<string, string> Selects
        {
            get
            {
                if (BaseTheme == null)
                {
                    return _selects;
                }

                var baseSelects = BaseTheme.Selects;
                var merged = new Multimap<string, string>();
                baseSelects.Each(x => merged.AddRange(x.Key, x.Value));
                foreach (var localSelect in _selects)
                {
                    if (!merged.ContainsKey(localSelect.Key))
                    {
                        // New Select in child: add to list.
                        merged.AddRange(localSelect.Key, localSelect.Value);
                    }
                    else
                    {
                        // Do nothing: we don't support overriding Selects
                    }
                }

                return merged;
            }
            internal set => _selects = value;
        }

        private ThemeDescriptorState _state;
        public ThemeDescriptorState State
        {
            get
            {
                if (_state == ThemeDescriptorState.Active)
                {
                    // Active state does not mean, that it actually IS active: check state of base themes!
                    var baseTheme = BaseTheme;
                    while (baseTheme != null)
                    {
                        if (baseTheme.State != ThemeDescriptorState.Active)
                        {
                            return baseTheme.State;
                        }
                        baseTheme = baseTheme.BaseTheme;
                    }
                }

                return _state;
            }
            protected internal set => _state = value;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                DetachFileWatchers();

                BaseTheme = null;
                if (_variables != null)
                {
                    foreach (var pair in _variables)
                    {
                        pair.Value.Dispose();
                    }
                    _variables.Clear();
                }
            }
        }

        ~ThemeDescriptor()
        {
            Dispose(false);
        }

        public override string ToString()
            => "{0} (Parent: {1}, State: {2})".FormatInvariant(Name, BaseThemeName ?? "-", State.ToString());

        public override bool Equals(object obj)
        {
            var other = obj as ThemeDescriptor;
            return other != null &&
                Name != null &&
                Name.EqualsNoCase(other.Name);
        }

        public override int GetHashCode()
            => Name.GetHashCode();
    }
}
