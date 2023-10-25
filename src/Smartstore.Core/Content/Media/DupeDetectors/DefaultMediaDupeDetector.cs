namespace Smartstore.Core.Content.Media
{
    public partial class DefaultMediaDupeDetector : IMediaDupeDetector
    {
        private readonly IMediaSearcher _searcher;
        private readonly int _folderId;

        public DefaultMediaDupeDetector(IMediaSearcher searcher, int folderId)
        {
            _searcher = searcher;
            _folderId = folderId;
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

        public async Task<HashSet<string>> GetAllFileNamesAsync(CancellationToken cancelToken = default)
        {
            // TODO: (mg) Bad access strategy. Loading thousands of file names repeatedly from database and
            // putting them to a hashset may not perform well under circumstances.
            // I think we also need to isolate CheckUniqueFileName in IMediaDupeDetector (instead of passing hashsets around).
            // This class can then decide how to obtain and store file names. I assume caching up to 30K-40K file names
            // should be ok performance and memory wise, even if we are in uncached mode.
            var query = _searcher.PrepareQuery(new() { FolderId = _folderId }, MediaLoadFlags.AsNoTracking);
            var fileNames = await query.Select(x => x.Name).ToListAsync(cancelToken);

            return new HashSet<string>(fileNames, StringComparer.CurrentCultureIgnoreCase);
        }
    }
}
