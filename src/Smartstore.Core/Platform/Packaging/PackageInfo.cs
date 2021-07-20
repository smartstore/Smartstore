using System;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Packaging
{
    public class PackageInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Type { get; set; }
        public string Path { get; set; }
        public IExtensionDescriptor ExtensionDescriptor { get; set; }
    }
}
