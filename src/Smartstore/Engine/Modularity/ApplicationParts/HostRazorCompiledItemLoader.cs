using Microsoft.AspNetCore.Razor.Hosting;

namespace Smartstore.Engine.Modularity.ApplicationParts
{
    /// <summary>
    /// A custom implementation of <see cref="RazorCompiledItemLoader"/> that
    /// fixes checksum validation issues. See https://github.com/dotnet/razor/issues/7498
    /// </summary>
    internal class HostRazorCompiledItemLoader : RazorCompiledItemLoader
    {
        protected override RazorCompiledItem CreateItem(RazorCompiledItemAttribute attribute)
        {
            return new CompiledItem(base.CreateItem(attribute));
        }

        class CompiledItem : RazorCompiledItem
        {
            public CompiledItem(RazorCompiledItem other)
            {
                Identifier = other.Identifier;
                Kind = other.Kind;
                Metadata = other.Metadata.Select(NormalizeMetadata).ToArray();
                Type = other.Type;
            }

            private object NormalizeMetadata(object metadata)
            {
                if (metadata is IRazorSourceChecksumMetadata checksum)
                {
                    return new RazorSourceChecksumAttribute(
                        checksum.ChecksumAlgorithm.ToUpper(), // ToUpper: fix the algorithm casing mismatch
                        checksum.Checksum,
                        checksum.Identifier);
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
