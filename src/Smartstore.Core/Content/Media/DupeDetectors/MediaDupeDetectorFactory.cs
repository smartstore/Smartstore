#nullable enable

using Smartstore.Core.Common.Configuration;

namespace Smartstore.Core.Content.Media
{
    /// <summary>
    /// Responsible for resolving <see cref="MediaFile"/> duplicate detectors (<see cref="IMediaDupeDetector"/>).
    /// </summary>
    public interface IMediaDupeDetectorFactory
    {
        /// <summary>
        /// Gets the most suitable detector to find <see cref="MediaFile"/> duplicates (uses <see cref="PerformanceSettings.MaxDupeDetectorCachedFiles"/>).
        /// </summary>
        /// <param name="folderId"><see cref="MediaFolderNode.Id"/> of the folder to be checked.</param>
        Task<IMediaDupeDetector> GetMediaDupeDetectorAsync(int folderId);
    }


    public partial class MediaDupeDetectorFactory : IMediaDupeDetectorFactory
    {
        private readonly IEnumerable<IMediaDupeDetector> _detectors;
        private readonly IMediaSearcher _searcher;
        private readonly PerformanceSettings _performanceSettings;

        public MediaDupeDetectorFactory(
            IEnumerable<IMediaDupeDetector> detectors,
            IMediaSearcher searcher,
            PerformanceSettings performanceSettings)
        {
            _detectors = detectors;
            _searcher = searcher;
            _performanceSettings = performanceSettings;
        }

        public async Task<IMediaDupeDetector> GetMediaDupeDetectorAsync(int folderId)
        {
            Guard.NotZero(folderId);

            var query = _searcher.PrepareQuery(new() { FolderId = folderId }, MediaLoadFlags.AsNoTracking);
            var usesCache = await query.CountAsync() <= _performanceSettings.MaxDupeDetectorCachedFiles;

            var dupeDetector = _detectors
                .OrderByDescending(x => x.Ordinal)
                .FirstOrDefault(x => x != null && x.UsesCache == usesCache);

            return dupeDetector!;
        }
    }
}
