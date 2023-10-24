#nullable enable

namespace Smartstore.Core.Content.Media
{
    public partial class DefaultMediaDupeDetector : IMediaDupeDetector
    {
        private readonly IMediaSearcher _searcher;

        public DefaultMediaDupeDetector(IMediaSearcher searcher)
        {
            _searcher = searcher;
        }

        public bool UsesCache => false;

        public int Ordinal => 0;

        public Task<MediaFile?> GetFileAsync(int folderId, string fileName, CancellationToken cancelToken = default)
        {
            Guard.NotZero(folderId);

            if (fileName.IsEmpty())
            {
                return null!;
            }

            var query = new MediaSearchQuery
            {
                FolderId = folderId,
                ExactMatch = true,
                Term = fileName,
                DeepSearch = false,
                IncludeAltForTerm = false
            };

            return _searcher.PrepareQuery(query, MediaLoadFlags.None).FirstOrDefaultAsync(cancelToken);
        }

        public async Task<HashSet<string>> GetFileNamesAsync(int folderId, CancellationToken cancelToken = default)
        {
            Guard.NotZero(folderId);

            var query = _searcher.PrepareQuery(new() { FolderId = folderId }, MediaLoadFlags.AsNoTracking);
            var fileNames = await query.Select(x => x.Name).ToListAsync(cancelToken);

            return new HashSet<string>(fileNames, StringComparer.CurrentCultureIgnoreCase);
        }
    }
}
