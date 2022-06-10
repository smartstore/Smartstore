using Smartstore.Collections;
using Smartstore.Core.Theming;

namespace Smartstore.Admin.Models.Themes
{
    public class ThemeDescriptorModel : ITopologicSortable<string>
    {
        public string Name { get; set; }

        public string BaseTheme { get; set; }

        public string FriendlyName { get; set; }

        public string Description { get; set; }

        public string Author { get; set; }

        public string ProjectUrl { get; set; }

        public string Version { get; set; }

        public string PreviewImageUrl { get; set; }

        public bool IsConfigurable { get; set; }

        public bool IsActive { get; set; }

        public ThemeDescriptorState State { get; set; }

        string ITopologicSortable<string>.Key => Name;

        string[] ITopologicSortable<string>.DependsOn
        {
            get => BaseTheme.IsEmpty() ? null : new string[] { BaseTheme };
        }
    }
}
