using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Smartstore.Engine.Modularity;
using Smartstore.IO;

namespace Smartstore.Core.Packaging
{
    public class ExtensionPackage
    {
        const string ManifestFileName = "manifest.json";
        
        public ExtensionPackage(ZipArchive zipArchive)
            : this(zipArchive, null)
        {
        }

        internal ExtensionPackage(ZipArchive zipArchive, IExtensionDescriptor descriptor)
        {
            Archive = Guard.NotNull(zipArchive, nameof(zipArchive));

            if (descriptor == null)
            {
                var manifest = zipArchive.GetEntry(ManifestFileName);
                if (manifest == null)
                {
                    // TODO: (core) Throw typed message, catch in controller and notify with localized message. 
                    throw new ArgumentException("TODO", nameof(zipArchive));
                }

                // TODO: (core) Throw typed message if deserialization fails, catch in controller and notify with localized message. 
                using var stream = manifest.Open();
                descriptor = JsonConvert.DeserializeObject<BareExtensionDescriptor>(stream.AsString());
            }

            Descriptor = descriptor;
        }

        public IExtensionDescriptor Descriptor { get; }

        public ZipArchive Archive { get; }

        public  IEnumerable<ZipArchiveEntry> Entries
        {
            get => Archive.Entries.Where(x => !x.FullName.EqualsNoCase(ManifestFileName));
        }

        internal Task ExtractToAsync(IDirectory target)
        {
            // TODO: (core) Implement ExtensionPackage.ExtractToAsync()
            return Task.CompletedTask;
        }
    }
}
