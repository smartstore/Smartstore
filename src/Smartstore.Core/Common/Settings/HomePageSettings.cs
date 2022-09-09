using Smartstore.Core.Configuration;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Common.Settings
{
    public class HomePageSettings : ISettings
    {
        /// <summary>
        /// Gets or sets the homepage meta title.
        /// </summary>
        [LocalizedProperty]
        public string MetaTitle { get; set; }

        /// <summary>
        /// Gets or sets the homepage meta description.
        /// </summary>
        [LocalizedProperty]
        public string MetaDescription { get; set; }

        /// <summary>
        /// Gets or sets the homepage meta keywords.
        /// </summary>
        [LocalizedProperty]
        public string MetaKeywords { get; set; }
    }
}
