using Microsoft.Extensions.FileProviders;

namespace Smartstore.Engine.Modularity
{
    /// <summary>
    /// Marker interface for asset file provider.
    /// </summary>
    public interface IAssetFileProvider : IFileProvider
    {
        /// <summary>
        /// Adds a file provider for a given path prefix.
        /// </summary>
        /// <param name="pathPrefix">Path prefix, e.g.: "themes/", "modules/" etc.</param>
        void AddFileProvider(string pathPrefix, IFileProvider provider);

        /// <summary>
        /// Adds a file provider resolver delegate for a segmented path (Prefix + Segment, e.g. "Themes/Flex/").
        /// </summary>
        /// <param name="pathPrefix">Path prefix, e.g.: "themes/", "modules/" etc.</param>
        /// <param name="resolver">The provider resolver delegate. First string argument provides the next path segment.</param>
        void AddSegmentedFileProvider(string pathPrefix, Func<string, IApplicationContext, IFileProvider> resolver);
    }
}
