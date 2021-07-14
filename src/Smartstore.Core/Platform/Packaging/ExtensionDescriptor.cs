using System;

namespace Smartstore.Core.Packaging
{
    public class ExtensionDescriptor
    {
        /// <summary>
        /// Base path, "/Themes" or "/Modules"
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Directory name under base path.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The extension type: Module | Theme
        /// </summary>
        public string ExtensionType { get; set; }

        // Extension metadata
        public string Name { get; set; }
        public string Path { get; set; }
        public string Description { get; set; }
        public Version Version { get; set; }
        public Version MinAppVersion { get; set; }
        public string Author { get; set; }
        public string WebSite { get; set; }
        public string Tags { get; set; }
    }
}