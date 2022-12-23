using System.Xml;
using Smartstore.Collections;
using Smartstore.IO;

namespace Smartstore.Core.Theming
{
    internal class ThemeDirectoryData : ITopologicSortable<string>
    {
        public string Name { get; set; }

        public IDirectory Directory { get; set; }

        public IFile ConfigurationFile { get; set; }

        public XmlElement ConfigurationNode { get; set; }

        public bool IsSymbolicLink { get; set; }

        public string BaseTheme { get; set; }

        string ITopologicSortable<string>.Key
            => Name;

        string[] ITopologicSortable<string>.DependsOn
        {
            get => BaseTheme == null ? null : new[] { BaseTheme };
        }
    }
}
