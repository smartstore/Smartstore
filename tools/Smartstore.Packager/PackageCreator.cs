using Smartstore.Core.Packaging;
using Smartstore.Engine.Modularity;
using Smartstore.IO;

namespace Smartstore.Packager
{
    internal class PackageCreator
    {
        private readonly IFileSystem _contentRoot;
        private readonly IPackageBuilder _packageBuilder;
        private readonly string _outputPath;

        public PackageCreator(string rootPath, string outputPath)
        {
            _outputPath = outputPath;
            _contentRoot = new LocalFileSystem(rootPath);
            _packageBuilder = new PackageBuilder(_contentRoot);
        }

        public async Task<FileInfo> CreateExtensionPackageAsync(IExtensionDescriptor descriptor)
        {
            var package = await _packageBuilder.BuildPackageAsync(descriptor);
            return await SavePackageFileAsync(package);
        }

        private async Task<FileInfo> SavePackageFileAsync(ExtensionPackage package)
        {
            var fileName = package.FileName;

            if (!Directory.Exists(_outputPath))
            {
                Directory.CreateDirectory(_outputPath);
            }

            fileName = Path.Combine(_outputPath, fileName);

            using (var stream = File.Create(fileName))
            {
                await package.ArchiveStream.CopyToAsync(stream);
            }

            var fileInfo = new FileInfo(fileName);

            return fileInfo;
        }
    }
}
