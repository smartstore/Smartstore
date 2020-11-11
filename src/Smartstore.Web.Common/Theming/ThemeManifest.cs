using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Smartstore.Collections;
using Smartstore.IO;

namespace Smartstore.Web.Common.Theming
{
    public enum ThemeManifestState
    {
        MissingBaseTheme = -1,
        Active = 0
    }

    public class ThemeManifest : ComparableObject<ThemeManifest>, IDisposable
    {
        internal ThemeManifest()
        {
        }

        #region Methods

        public static ThemeManifest Create(string themeName, IFileSystem root)
        {
            Guard.NotEmpty(themeName, nameof(themeName));

            var directoryData = CreateThemeDirectoryData(root.GetDirectory(themeName), root);
            if (directoryData != null)
            {
                return Create(directoryData);
            }

            return null;
        }

        internal static ThemeManifest Create(ThemeDirectoryData directoryData)
        {
            Guard.NotNull(directoryData, nameof(directoryData));

            var materializer = new ThemeManifestMaterializer(directoryData);
            var manifest = materializer.Materialize();

            return manifest;
        }

        internal static ThemeDirectoryData CreateThemeDirectoryData(IDirectory themeDirectory, IFileSystem root)
        {
            if (!themeDirectory.Exists)
                return null;

            var isSymLink = themeDirectory.IsSymbolicLink(out var finalPathName);
            if (isSymLink)
            {
                themeDirectory = new LocalDirectory(themeDirectory.SubPath, new DirectoryInfo(finalPathName), root);
            }

            var themeConfigFile = root.GetFile(root.PathCombine(themeDirectory.Name, "theme.json"));

            if (themeConfigFile.Exists)
            {
                var configuration = JsonConvert.DeserializeObject<ThemeConfiguration>(themeConfigFile.ReadAllText());

                var baseTheme = configuration.BaseTheme.TrimSafe().NullEmpty();
                if (baseTheme != null && baseTheme.IsCaseInsensitiveEqual(themeDirectory.Name))
                {
                    // Don't let theme base on itself!
                    baseTheme = null;
                }

                return new ThemeDirectoryData
                {
                    Directory = themeDirectory,
                    ConfigurationFile = themeConfigFile,
                    IsSymbolicLink = isSymLink,
                    Configuration = configuration,
                    BaseTheme = baseTheme
                };
            }

            return null;
        }

        #endregion

        #region Properties

        public IFileSystem FileProvider
        {
            get;
            protected internal set;
        }

        public IFile ConfigurationFile
        {
            get;
            protected internal set;
        }

        public ThemeConfiguration Configuration
        {
            get;
            protected internal set;
        }

        /// <summary>
        /// Determines whether the theme directory is a symbolic link to another target.
        /// </summary>
        public bool IsSymbolicLink
        {
            get;
            protected internal set;
        }

        public string PreviewImagePath
        {
            get;
            protected internal set;
        }

        public string Description
        {
            get;
            protected internal set;
        }

        [ObjectSignature]
        public string ThemeName
        {
            get;
            protected internal set;
        }

        public string BaseThemeName
        {
            get;
            internal set;
        }

        public ThemeManifest BaseTheme
        {
            get;
            internal set;
        }

        public string ThemeTitle
        {
            get;
            protected internal set;
        }

        public string Author
        {
            get;
            protected internal set;
        }

        public string Url
        {
            get;
            protected internal set;
        }

        public string Version
        {
            get;
            protected internal set;
        }

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
                            Manifest = localVar.Value.Manifest
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

        private ThemeManifestState _state;
        public ThemeManifestState State
        {
            get
            {
                if (_state == ThemeManifestState.Active)
                {
                    // Active state does not mean, that it actually IS active: check state of base themes!
                    var baseTheme = BaseTheme;
                    while (baseTheme != null)
                    {
                        if (baseTheme.State != ThemeManifestState.Active)
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

        public override string ToString()
        {
            return "{0} (Parent: {1}, State: {2})".FormatInvariant(ThemeName, BaseThemeName ?? "-", State.ToString());
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
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

        ~ThemeManifest()
        {
            Dispose(false);
        }

        #endregion
    }
}
