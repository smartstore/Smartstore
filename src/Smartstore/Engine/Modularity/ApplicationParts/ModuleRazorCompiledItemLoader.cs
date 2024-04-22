using Microsoft.AspNetCore.Razor.Hosting;

namespace Smartstore.Engine.Modularity.ApplicationParts
{
    /// <summary>
    /// A custom implementation of <see cref="RazorCompiledItemLoader"/> that
    /// prefixes the unrooted view path with "/Modules/{ModuleName}"
    /// </summary>
    internal class ModuleRazorCompiledItemLoader : RazorCompiledItemLoader
    {
        private readonly string _moduleName;
        private readonly string _modulePath;

        public ModuleRazorCompiledItemLoader(string moduleName)
        {
            _moduleName = moduleName;
            _modulePath = "/Modules/" + _moduleName;
        }

        protected override RazorCompiledItem CreateItem(RazorCompiledItemAttribute attribute)
        {
            return new CompiledItem(_modulePath, base.CreateItem(attribute));
        }

        class CompiledItem : RazorCompiledItem
        {
            private readonly string _pathPrefix;

            public CompiledItem(string pathPrefix, RazorCompiledItem other)
            {
                _pathPrefix = pathPrefix;

                Identifier = pathPrefix + other.Identifier;
                Kind = other.Kind;
                Metadata = other.Metadata.Select(NormalizeMetadata).ToArray();
                Type = other.Type;
            }

            private object NormalizeMetadata(object metadata)
            {
                if (metadata is IRazorSourceChecksumMetadata checksum)
                {
                    return new RazorSourceChecksumAttribute(
                        checksum.ChecksumAlgorithm.ToUpper(),
                        checksum.Checksum,
                        // We have to prefix the checksum identifier - actually the unrooted path to the view file, starting with "/Views/..." -
                        // with the module's root path, e.g. "/Modules/Smartstore.Blog". Otherwise checksum validation will fail
                        // and the compiled view's source file will never be watched for changes.
                        _pathPrefix + checksum.Identifier);
                }
                else if (metadata is RazorCompiledItemMetadataAttribute attr)
                {
                    return new RazorCompiledItemMetadataAttribute(attr.Key, _pathPrefix + attr.Value);
                }

                return metadata;
            }

            public override string Identifier { get; }
            public override string Kind { get; }
            public override IReadOnlyList<object> Metadata { get; }
            public override Type Type { get; }
        }
    }
}
