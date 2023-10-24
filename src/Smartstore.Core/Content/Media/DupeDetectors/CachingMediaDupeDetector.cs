#nullable enable

namespace Smartstore.Core.Content.Media
{
    public partial class CachingMediaDupeDetector : IMediaDupeDetector
    {
        private readonly IMediaSearcher _searcher;
        private Dictionary<int, Dictionary<string, MediaFile>>? _cachedFilesByFolder;

        public CachingMediaDupeDetector(IMediaSearcher searcher)
        {
            _searcher = searcher;
        }

        public bool UsesCache => true;

        public int Ordinal => 0;

        public async Task<MediaFile?> GetFileAsync(int folderId, string fileName, CancellationToken cancelToken = default)
        {
            Guard.NotZero(folderId);

            if (fileName.IsEmpty())
            {
                return null;
            }

            var files = await GetFiles(folderId, cancelToken);
            return files.Get(fileName);
        }

        public async Task<HashSet<string>> GetFileNamesAsync(int folderId, CancellationToken cancelToken = default)
        {
            Guard.NotZero(folderId);

            var files = await GetFiles(folderId, cancelToken);
            return new HashSet<string>(files.Keys, StringComparer.CurrentCultureIgnoreCase);
        }

        private async Task<Dictionary<string, MediaFile>> GetFiles(int folderId, CancellationToken cancelToken)
        {
            _cachedFilesByFolder ??= new();

            if (!_cachedFilesByFolder.TryGetValue(folderId, out var files))
            {
                files = (await _searcher
                    .SearchFiles(new() { FolderId = folderId }, MediaLoadFlags.None).LoadAsync(false, cancelToken))
                    .ToDictionarySafe(x => x.Name);

                _cachedFilesByFolder[folderId] = files;
            }

            return files;
        }
    }
}
