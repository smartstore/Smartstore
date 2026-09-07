using Smartstore.Core.Packaging;
using Smartstore.Engine.Modularity;
using Smartstore.IO;

namespace Smartstore.Packager
{
    /// <summary>
    /// Creates deployable extension packages and writes them to an output directory.
    /// </summary>
    public class PackageCreator
    {
        private readonly IFileSystem _contentRoot;
        private readonly IPackageBuilder _packageBuilder;
        private readonly string _outputPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageCreator"/> class.
        /// </summary>
        /// <param name="rootPath">Physical path of the build artifact root directory.</param>
        /// <param name="outputPath">Physical path of the directory to write package files to.</param>
        public PackageCreator(string rootPath, string outputPath)
        {
            _outputPath = outputPath;
            _contentRoot = new LocalFileSystem(rootPath);
            _packageBuilder = new PackageBuilder(_contentRoot);
        }

        /// <summary>
        /// Builds a package for the given extension and saves it to the output directory.
        /// </summary>
        /// <param name="descriptor">The extension to package.</param>
        /// <returns>The created package file.</returns>
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
