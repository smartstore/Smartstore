using System;
using System.Collections.Generic;
using System.Linq;
using Smartstore.Collections;
using Smartstore.IO;

namespace Smartstore.Web.Common.Theming
{
    internal class ThemeManifestMaterializer
    {
        private readonly ThemeManifest _manifest;
        private readonly ThemeDirectoryData _directoryData;

        public ThemeManifestMaterializer(ThemeDirectoryData directoryData)
        {
            Guard.NotNull(directoryData, nameof(directoryData));

            _directoryData = directoryData;
            _manifest = new ThemeManifest
            {
                ThemeName = directoryData.Directory.Name,
                ConfigurationFile = directoryData.ConfigurationFile,
                Configuration = directoryData.Configuration,
                IsSymbolicLink = directoryData.IsSymbolicLink,
                BaseThemeName = directoryData.BaseTheme,
                FileProvider = new LocalFileSystem(directoryData.Directory.PhysicalPath),
            };
        }

        public ThemeManifest Materialize()
        {
            var cfg = _manifest.Configuration;

            _manifest.ThemeTitle = cfg.Title.NullEmpty() ?? _manifest.ThemeName;
            _manifest.PreviewImagePath = cfg.PreviewImagePath.NullEmpty() ?? _directoryData.Directory.SubPath + "/preview.png";
            _manifest.Description = cfg.Description.ToSafe();
            _manifest.Author = cfg.Author.ToSafe();
            _manifest.Url = cfg.Url.ToSafe();
            _manifest.Version = cfg.Version.NullEmpty() ?? "1.0";

            _manifest.Selects = MaterializeSelects();
            _manifest.Variables = MaterializeVariables();

            return _manifest;
        }

        private Multimap<string, string> MaterializeSelects()
        {
            var selects = new Multimap<string, string>();
            var cfg = _manifest.Configuration;
            var cfgSelects = cfg.Selects;

            if (cfgSelects == null)
            {
                return selects;
            }

            foreach (var kvp in cfgSelects)
            {
                var options = kvp.Value;
                if (options == null || !options.Any())
                {
                    throw new SmartException("A 'Select' element must contain at least one 'Option' child element. Affected: '{0}' - element: {1}", _manifest.FileProvider.Root, kvp.Key);
                }

                foreach (var option in options)
                {
                    if (option.IsEmpty())
                    {
                        throw new SmartException("A select option cannot be empty. Affected: '{0}' - element: {1}", _manifest.FileProvider.Root, kvp.Key);
                    }

                    selects.Add(kvp.Key, option);
                }

            }

            return selects;
        }

        private IDictionary<string, ThemeVariableInfo> MaterializeVariables()
        {
            var vars = new Dictionary<string, ThemeVariableInfo>(StringComparer.OrdinalIgnoreCase);
            var cfg = _manifest.Configuration;
            var cfgVars = cfg.Variables;

            if (cfgVars == null)
            {
                return vars;
            }

            foreach (var kvp in cfgVars)
            {
                var info = MaterializeVariable(kvp.Key, kvp.Value);
                if (info != null && info.Name.HasValue())
                {
                    vars.Add(info.Name, info);
                }
            }

            return vars;
        }

        private ThemeVariableInfo MaterializeVariable(string name, ThemeConfiguration.VariableConfiguration cfg)
        {
            string type = cfg.Type.NullEmpty() ?? "String";

            var varType = ConvertVarType(type, name, out var selectRef);

            if (varType != ThemeVariableType.String && cfg.Value.IsEmpty())
            {
                throw new SmartException("A value is required for non-string 'Var' elements. Affected: '{0}' - element: {1}", _manifest.FileProvider.Root, name);
            }

            var info = new ThemeVariableInfo
            {
                Name = name,
                DefaultValue = cfg.Value,
                Type = varType,
                SelectRef = selectRef,
                Manifest = _manifest
            };

            return info;
        }

        private ThemeVariableType ConvertVarType(string type, string name, out string selectRef)
        {
            var result = ThemeVariableType.String;
            selectRef = null;

            if (type.ToLower().StartsWith("select", StringComparison.CurrentCultureIgnoreCase))
            {
                var arr = type.Split('#');
                if (arr.Length < 1 || arr[1].IsEmpty())
                {
                    throw new SmartException("The 'id' of a select element must be provided (pattern: Select#MySelect). Affected: '{0}' - element: {1}", _manifest.FileProvider.Root, name);
                }

                selectRef = arr[1];
                return ThemeVariableType.Select;
            }

            switch (type.ToLowerInvariant())
            {
                case "string":
                    result = ThemeVariableType.String;
                    break;
                case "color":
                    result = ThemeVariableType.Color;
                    break;
                case "boolean":
                    result = ThemeVariableType.Boolean;
                    break;
                case "number":
                    result = ThemeVariableType.Number;
                    break;
            }

            return result;
        }
    }
}