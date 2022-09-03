using System.IO.Compression;
using Newtonsoft.Json;
using Smartstore.Engine.Modularity;
using Smartstore.IO;
using Smartstore.Utilities;

namespace Smartstore.Core.Packaging
{
    public partial class PackageBuilder : IPackageBuilder
    {
        private static readonly Wildcard[] _ignoredPaths = new[]
        {
            "/obj/*", "/ref/*", "/refs/*",
            "*.obj", "*.pdb", "*.exclude", "*.cs", "*.deps.json"
        }.Select(x => new Wildcard(x)).ToArray();

        private readonly IFileSystem _contentRoot;

        public PackageBuilder(IApplicationContext appContext)
        {
            _contentRoot = appContext.ContentRoot;
        }

        public PackageBuilder(IFileSystem contentRoot)
        {
            _contentRoot = Guard.NotNull(contentRoot, nameof(contentRoot));
        }

        public async Task<ExtensionPackage> BuildPackageAsync(IExtensionDescriptor extension)
        {
            Guard.NotNull(extension, nameof(extension));

            if (extension is not IExtensionLocation)
            {
                throw new InvalidExtensionException($"Extension '{extension.Name}' cannot be packaged because it cannot be located on local file system.");
            }

            var manifest = new MinimalExtensionDescriptor(extension);

            var package = new ExtensionPackage(new MemoryStream(), manifest, true);

            using (var archive = new ZipArchive(package.ArchiveStream, ZipArchiveMode.Create, true))
            {
                // Embed core manifest file
                await EmbedManifest(archive, manifest);

                // Embed all extension files
                await EmbedFiles(archive, extension);
            }

            package.ArchiveStream.Seek(0, SeekOrigin.Begin);

            return package;
        }

        private static async Task EmbedManifest(ZipArchive archive, MinimalExtensionDescriptor manifest)
        {
            var json = JsonConvert.SerializeObject(manifest, Formatting.Indented);

            var memStream = new MemoryStream();
            using (var streamWriter = new StreamWriter(memStream, leaveOpen: true))
            {
                await streamWriter.WriteAsync(json);
            }

            memStream.Seek(0, SeekOrigin.Begin);
            await CreateArchiveEntry(archive, PackagingUtility.ManifestFileName, memStream);
        }

        private async Task EmbedFiles(ZipArchive archive, IExtensionDescriptor extension)
        {
            var location = extension as IExtensionLocation;

            foreach (var file in _contentRoot.EnumerateFiles(location.Path, deep: true))
            {
                // Skip ignores files
                if (IgnoreFile(file.SubPath))
                {
                    continue;
                }

                await CreateArchiveEntry(archive, file.SubPath, await file.OpenReadAsync());
            }
        }

        private static async Task CreateArchiveEntry(ZipArchive archive, string name, Stream source)
        {
            var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
            using var entryStream = entry.Open();

            using (source)
            {
                await source.CopyToAsync(entryStream);
            }
        }

        private static bool IgnoreFile(string filePath)
        {
            return string.IsNullOrEmpty(filePath) || _ignoredPaths.Any(x => x.IsMatch(filePath));
        }
    }
}
