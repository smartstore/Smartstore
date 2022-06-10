using System.IO.Compression;
using Newtonsoft.Json;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Packaging
{
    public class ExtensionPackage : Disposable
    {
        private string _fileName;
        private readonly bool _leaveOpen;

        public ExtensionPackage(Stream archiveStream, bool leaveOpen)
            : this(archiveStream, null, leaveOpen)
        {
        }

        internal ExtensionPackage(Stream archiveStream, IExtensionDescriptor descriptor, bool leaveOpen)
        {
            ArchiveStream = Guard.NotNull(archiveStream, nameof(archiveStream));
            _leaveOpen = leaveOpen;

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
            get => _fileName ??= Descriptor.BuildPackageName() + ".zip";
            init => _fileName = value;
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing && !_leaveOpen)
                ArchiveStream.Dispose();
        }
    }
}
