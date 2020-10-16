using System;
using Smartstore.Collections;
using Smartstore.IO;

namespace Smartstore.Web.Common.Theming
{
    internal class ThemeDirectoryData : ITopologicSortable<string>
    {
        public IDirectory Directory { get; set; }

        public IFile ConfigurationFile { get; set; }

        public bool IsSymbolicLink { get; set; }

        public ThemeConfiguration Configuration { get; set; }

        public string BaseTheme { get; set; }

        string ITopologicSortable<string>.Key 
            => Directory.Name;

        string[] ITopologicSortable<string>.DependsOn
        {
            get => BaseTheme == null ? null : new[] { BaseTheme };
        }
    }
}
