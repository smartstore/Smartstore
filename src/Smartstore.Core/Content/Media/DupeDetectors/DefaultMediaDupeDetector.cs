namespace Smartstore.Core.Content.Media
{
    public partial class DefaultMediaDupeDetector : IMediaDupeDetector
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

        public Task<MediaFile> DetectFileAsync(string fileName, CancellationToken cancelToken = default)
        {
            if (fileName.IsEmpty())
            {
                return null!;
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

        public async Task<string> GetUniqueFileNameAsync(string title, string ext, CancellationToken cancelToken = default)
        {
            Guard.NotEmpty(title);
            Guard.NotEmpty(ext);

            ICollection<string> destFileNames;

            if (_fileCount > MaxCachedFileNames)
            {
                // Load file names starting with 'title' but do not cache them.
                var query = _searcher.PrepareQuery(new MediaSearchQuery
                {
                    FolderId = _folderId,
                    Term = title + '*',
                    Deleted = null
                }, MediaLoadFlags.AsNoTracking);

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

            MediaHelper.CheckUniqueFileName(title, ext, destFileNames, out var uniqueName);
            return uniqueName;
        }
    }
}
