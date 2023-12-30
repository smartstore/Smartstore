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
        /// Gets the most suitable detector to find <see cref="MediaFile"/> duplicates (uses <see cref="PerformanceSettings.MediaDupeDetectorMaxCacheSize"/>).
        /// </summary>
        /// <param name="folderId"><see cref="MediaFolderNode.Id"/> of the folder to check for duplicates in.</param>
        IMediaDupeDetector GetDetector(int folderId);
    }

    public partial class MediaDupeDetectorFactory : IMediaDupeDetectorFactory
    {
        private readonly IMediaSearcher _searcher;
        private readonly PerformanceSettings _performanceSettings;

        private readonly Dictionary<int, IMediaDupeDetector> _detectors = [];

        public MediaDupeDetectorFactory(IMediaSearcher searcher, PerformanceSettings performanceSettings)
        {
            _searcher = searcher;
            _performanceSettings = performanceSettings;
        }

        public IMediaDupeDetector GetDetector(int folderId)
        {
            Guard.NotZero(folderId);

            if (!_detectors.TryGetValue(folderId, out var detector))
            {
                var query = _searcher.PrepareQuery(new() { FolderId = folderId }, MediaLoadFlags.AsNoTracking);
                var fileCount = query.Count();

                detector = fileCount <= _performanceSettings.MediaDupeDetectorMaxCacheSize
                    ? new CachingMediaDupeDetector(_searcher, folderId)
                    : new DefaultMediaDupeDetector(_searcher, folderId, fileCount);

                _detectors[folderId] = detector;
            }

            return detector!;
        }
    }
}
