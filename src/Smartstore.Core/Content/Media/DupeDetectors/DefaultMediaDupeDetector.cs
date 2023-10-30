namespace Smartstore.Core.Content.Media
{
    internal class DefaultMediaDupeDetector : MediaDupeDetectorBase
    {
        const int MaxCachedFileNames = 40000;

        private readonly IMediaSearcher _searcher;
        private readonly int _folderId;
        private readonly int _fileCount;

        private HashSet<string> _cachedFileNames;

        public DefaultMediaDupeDetector(IMediaSearcher searcher, int folderId, int fileCount)
        {
            _searcher = searcher;
            _folderId = folderId;
            _fileCount = fileCount;
        }

        public override Task<MediaFile> DetectFileAsync(string fileName, CancellationToken cancelToken = default)
        {
            if (fileName.IsEmpty())
            {
                return null;
            }

            var query = new MediaSearchQuery
            {
                FolderId = _folderId,
                ExactMatch = true,
                Term = fileName,
                DeepSearch = false,
                IncludeAltForTerm = false
            };

            return _searcher.PrepareQuery(query, MediaLoadFlags.None).FirstOrDefaultAsync(cancelToken);
        }

        public override async Task<string> GetUniqueFileNameAsync(string title, string extension, CancellationToken cancelToken = default)
        {
            Guard.NotEmpty(title);
            Guard.NotEmpty(extension);

            ICollection<string> destFileNames;

            if (_fileCount > MaxCachedFileNames)
            {
                // (perf) First make fast check (exact match is still faster than StartsWith). The chance that there's no dupe is much higher.
                var q = new MediaSearchQuery
                {
                    FolderId = _folderId,
                    Term = title + '.' + extension,
                    ExactMatch = true,
                    Deleted = null
                };

                var query = _searcher.PrepareQuery(q, MediaLoadFlags.AsNoTracking);
                var exists = await query.AnyAsync(cancelToken);
                if (!exists)
                {
                    return null;
                }

                // Load file names starting with 'title' but do not cache them.
                q.Term = title + '*';
                q.ExactMatch = false;

                query = _searcher.PrepareQuery(q, MediaLoadFlags.AsNoTracking);

                destFileNames = await query.Select(x => x.Name).ToListAsync(cancelToken);
            }
            else
            {
                // Get all file names of the folder in one go and cache them.
                if (_cachedFileNames == null)
                {
                    var query = _searcher.PrepareQuery(new() { FolderId = _folderId }, MediaLoadFlags.AsNoTracking);
                    var fileNames = await query.Select(x => x.Name).ToListAsync(cancelToken);

                    _cachedFileNames = new(fileNames, StringComparer.CurrentCultureIgnoreCase);
                }

                destFileNames = _cachedFileNames;
            }

            return GetUniqueFileName(title, extension, destFileNames);
        }
    }
}
