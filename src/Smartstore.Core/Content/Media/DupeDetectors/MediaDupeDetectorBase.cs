namespace Smartstore.Core.Content.Media
{
    internal abstract class MediaDupeDetectorBase : IMediaDupeDetector
    {
        private readonly HashSet<string> _checkedNames = new(StringComparer.OrdinalIgnoreCase);

        public abstract Task<MediaFile> DetectFileAsync(string fileName, CancellationToken cancelToken = default);

        public abstract Task<string> GetUniqueFileNameAsync(string title, string extension, CancellationToken cancelToken = default);

        protected string GetUniqueFileName(string title, string extension, ICollection<string> fileNames)
        {
            Guard.NotEmpty(title);
            Guard.NotEmpty(extension);
            Guard.NotNull(fileNames);

            if (fileNames.Count == 0)
            {
                return null;
            }

            ICollection<string> destFileNames = _checkedNames.Count > 0
                ? fileNames.Concat(_checkedNames).ToArray()
                : fileNames;

            if (MediaHelper.CheckUniqueFileName(title, extension, destFileNames, out var uniqueName))
            {
                _checkedNames.Add(uniqueName);
            }

            return uniqueName;
        }

        public void Dispose()
        {
            _checkedNames.Clear();
        }
    }
}
