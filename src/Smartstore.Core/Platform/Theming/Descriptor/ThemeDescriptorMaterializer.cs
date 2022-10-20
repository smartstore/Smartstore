using System.Xml;
using Smartstore.Collections;

namespace Smartstore.Core.Theming
{
    internal class ThemeDescriptorMaterializer
    {
        private readonly ThemeDescriptor _descriptor;
        private readonly ThemeDirectoryData _directoryData;

        public ThemeDescriptorMaterializer(ThemeDirectoryData directoryData)
        {
            Guard.NotNull(directoryData, nameof(directoryData));

            _directoryData = directoryData;
            _descriptor = new ThemeDescriptor
            {
                Name = directoryData.Directory.Name,
                ConfigurationFile = directoryData.ConfigurationFile,
                IsSymbolicLink = directoryData.IsSymbolicLink,
                BaseThemeName = directoryData.BaseTheme,
                Path = "/Themes/" + directoryData.Directory.Name + "/",
                PhysicalPath = directoryData.Directory.PhysicalPath
            };
        }

        public ThemeDescriptor Materialize()
        {
            var root = _directoryData.ConfigurationNode;

            _descriptor.FriendlyName = root.GetAttribute("title").NullEmpty() ?? _descriptor.Name;
            _descriptor.PreviewImagePath = root.GetAttribute("previewImagePath").NullEmpty() ?? "images/preview.png";
            _descriptor.Description = root.GetAttribute("description").NullEmpty();
            _descriptor.Author = root.GetAttribute("author").NullEmpty();
            _descriptor.ProjectUrl = root.GetAttribute("url").NullEmpty();
            _descriptor.Version = new Version(root.GetAttribute("version").NullEmpty() ?? "1.0");
            _descriptor.MinAppVersion = SmartstoreVersion.Version;

            _descriptor.Selects = MaterializeSelects();
            _descriptor.Variables = MaterializeVariables();

            return _descriptor;
        }

        private Multimap<string, string> MaterializeSelects()
        {
            var selects = new Multimap<string, string>();
            var root = _directoryData.ConfigurationNode;
            var xndSelects = root.SelectNodes(@"Selects/Select").Cast<XmlElement>();

            foreach (var xel in xndSelects)
            {
                string id = xel.GetAttribute("id").ToSafe();
                if (id.IsEmpty() || selects.ContainsKey(id))
                {
                    throw new InvalidOperationException($"A 'Select' element must contain a unique id. Affected: '{_descriptor.PhysicalPath}' - element: {xel.OuterXml}");
                }

                var xndOptions = xel.SelectNodes(@"Option").Cast<XmlElement>();
                if (!xndOptions.Any())
                {
                    throw new InvalidOperationException($"A 'Select' element must contain at least one 'Option' child element. Affected: '{_descriptor.PhysicalPath}' - element: {xel.OuterXml}");
                }

                foreach (var xelOption in xndOptions)
                {
                    string option = xelOption.InnerText;
                    if (option.IsEmpty())
                    {
                        throw new InvalidOperationException($"A select option cannot be empty. Affected: '{_descriptor.PhysicalPath}' - element: {xel.OuterXml}");
                    }

                    selects.Add(id, option);
                }

            }

            return selects;
        }

        private IDictionary<string, ThemeVariableInfo> MaterializeVariables()
        {
            var vars = new Dictionary<string, ThemeVariableInfo>(StringComparer.OrdinalIgnoreCase);
            var root = _directoryData.ConfigurationNode;
            var xndVars = root.SelectNodes(@"Vars/Var").Cast<XmlElement>();

            foreach (var xel in xndVars)
            {
                var info = MaterializeVariable(xel);
                if (info != null && info.Name.HasValue())
                {
                    if (vars.ContainsKey(info.Name))
                    {
                        throw new InvalidOperationException($"Duplicate variable name '{info.Name}' in '{_descriptor.PhysicalPath}'. Variable names must be unique.");
                    }
                    vars.Add(info.Name, info);
                }
            }

            return vars;
        }

        private ThemeVariableInfo MaterializeVariable(XmlElement xel)
        {
            string name = xel.GetAttribute("name");
            string value = xel.InnerText;

            if (name.IsEmpty())
            {
                throw new InvalidOperationException($"The name attribute is required for the 'Var' element. Affected: '{_descriptor.PhysicalPath}' - element: {xel.OuterXml}");
            }

            string type = xel.GetAttribute("type").ToSafe("String");

            var varType = ConvertVarType(type, xel, out var selectRef);

            if (varType != ThemeVariableType.String && value.IsEmpty())
            {
                throw new InvalidOperationException($"A value is required for non-string 'Var' elements. Affected: '{_descriptor.PhysicalPath}' - element: {xel.OuterXml}");
            }

            var info = new ThemeVariableInfo
            {
                Name = name,
                DefaultValue = value,
                Type = varType,
                SelectRef = selectRef,
                ThemeDescriptor = _descriptor
            };

            return info;
        }

        private ThemeVariableType ConvertVarType(string type, XmlElement affected, out string selectRef)
        {
            ThemeVariableType result = ThemeVariableType.String;
            selectRef = null;

            if (type.ToLower().StartsWith("select", StringComparison.CurrentCultureIgnoreCase))
            {
                var arr = type.Split(new char[] { '#' });
                if (arr.Length < 1 || arr[1].IsEmpty())
                {
                    throw new InvalidOperationException(
                        $"The 'id' of a select element must be provided (pattern: Select#MySelect). Affected: '{_descriptor.PhysicalPath}' - element: {affected.OuterXml}");
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