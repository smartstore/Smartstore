using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Content.Media.Storage
{
    public interface IMediaMover
    {
        /// <summary>
        /// Moves media items from one storage provider to another
        /// </summary>
        /// <param name="sourceProvider">The source media storage provider</param>
        /// <param name="targetProvider">The target media storage provider</param>
        /// <returns><c>true</c> success, <c>failure</c></returns>
        Task<bool> MoveAsync(
            Provider<IMediaStorageProvider> sourceProvider,
            Provider<IMediaStorageProvider> targetProvider,
            CancellationToken cancelToken = default);
    }
}
