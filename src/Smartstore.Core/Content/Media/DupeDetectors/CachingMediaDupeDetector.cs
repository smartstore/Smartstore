namespace Smartstore.Core.Content.Media
{
    internal class CachingMediaDupeDetector : IMediaDupeDetector
    {
        private readonly IMediaSearcher _searcher;
        private readonly int _folderId;

        private Dictionary<int, Dictionary<string, MediaFile>> _cachedFilesByFolder;

        public CachingMediaDupeDetector(IMediaSearcher searcher, int folderId)
        {
            _searcher = searcher;
            _folderId = folderId;
        }

        public async Task<MediaFile> DetectFileAsync(string fileName, CancellationToken cancelToken = default)
        {
            if (fileName.IsEmpty())
            {
                return null;
            }

            var files = await GetFiles(cancelToken);
            return files.Get(fileName);
        }

        public async Task<string> GetUniqueFileNameAsync(string title, string ext, CancellationToken cancelToken = default)
        {
            Guard.NotEmpty(title);
            Guard.NotEmpty(ext);

            var files = await GetFiles(cancelToken);
            MediaHelper.CheckUniqueFileName(title, ext, files.Keys, out var uniqueName);

            return uniqueName;
        }

        private async Task<Dictionary<string, MediaFile>> GetFiles(CancellationToken cancelToken)
        {
            _cachedFilesByFolder ??= new();

            if (!_cachedFilesByFolder.TryGetValue(_folderId, out var files))
            {
                files = (await _searcher
                    .SearchFiles(new() { FolderId = _folderId }, MediaLoadFlags.None).LoadAsync(false, cancelToken))
                    .ToDictionarySafe(x => x.Name);

                _cachedFilesByFolder[_folderId] = files;
            }

            return files;
        }
    }
}
