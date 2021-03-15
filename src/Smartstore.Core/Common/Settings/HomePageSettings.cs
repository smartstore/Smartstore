using Smartstore.Core.Configuration;

namespace Smartstore.Core.Common.Settings
{
    public class HomePageSettings : ISettings
    {
        /// <summary>
        /// Gets or sets the homepage meta title.
        /// </summary>
        public string MetaTitle { get; set; }

        /// <summary>
        /// Gets or sets the homepage meta description.
        /// </summary>
        public string MetaDescription { get; set; }

        /// <summary>
        /// Gets or sets the homepage meta keywords.
        /// </summary>
        public string MetaKeywords { get; set; }
    }
}
