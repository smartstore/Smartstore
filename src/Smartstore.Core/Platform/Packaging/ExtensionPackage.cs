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
    public class ExtensionPackage : Disposable
    {
        private string _fileName;

        public ExtensionPackage(Stream archiveStream)
            : this(archiveStream, null)
        {
        }

        internal ExtensionPackage(Stream archiveStream, IExtensionDescriptor descriptor)
        {
            ArchiveStream = Guard.NotNull(archiveStream, nameof(archiveStream));

            if (archiveStream.CanSeek)
            {
                archiveStream.Seek(0, SeekOrigin.Begin);
            }

            if (descriptor == null)
            {
                using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, true);
                var manifest = archive.GetEntry(PackagingUtility.ManifestFileName);
                if (manifest == null)
                {
                    // TODO: (core) Throw typed message, catch in controller and notify with localized message. 
                    throw new ArgumentException("TODO", nameof(archiveStream));
                }

                // TODO: (core) Throw typed message if deserialization fails, catch in controller and notify with localized message. 
                using var stream = manifest.Open();
                descriptor = JsonConvert.DeserializeObject<MinimalExtensionDescriptor>(stream.AsString());
            }

            Descriptor = descriptor;
        }

        public Stream ArchiveStream { get; }

        public IExtensionDescriptor Descriptor { get; }

        public string FileName
        {
            get => _fileName ??= PackagingUtility.BuildPackageFileName(Descriptor);
            init => _fileName = value;
        }

        public Task ExtractToDirectoryAsync(IDirectory target)
        {
            // TODO: (core) Implement ExtensionPackage.ExtractToAsync()
            return Task.CompletedTask;
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
                ArchiveStream.Dispose();
        }
    }
}
