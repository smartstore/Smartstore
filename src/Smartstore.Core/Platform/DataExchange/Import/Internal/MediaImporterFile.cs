using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Media.Storage;
using Smartstore.Net.Http;

namespace Smartstore.Core.DataExchange.Import.Internal
{
    internal class MediaImporterFile : Disposable
    {
        private bool _fileExists;

        public bool Init(DownloadManagerItem item, Dictionary<string, FileBatchSource> files)
        {
            if (files.TryGetValue(item.Path, out var fileSource))
            {
                // File was already processed within batch.
                _fileExists = true;
                Stream = fileSource.Source.SourceStream;
            }
            else
            {
                _fileExists = false;
                Stream = File.OpenRead(item.Path);
                if (Stream.Length <= 0)
                {
                    Stream.Dispose();
                    Stream = null;
                    return false;
                }
            }

            return true;
        }

        public static implicit operator Stream(MediaImporterFile obj)
            => obj.Stream;

        public Stream Stream { get; private set; }

        public void Add(DownloadManagerItem item, Dictionary<string, FileBatchSource> files)
        {
            if (_fileExists)
            {
                // Assign item.
                ((List<DownloadManagerItem>)files[item.Path].State).Add(item);
            }
            else
            {
                // Keep stream for later batch import of new images.
                files.Add(item.Path, new(MediaStorageItem.FromStream(Stream, true))
                {
                    FileName = item.FileName,
                    State = new List<DownloadManagerItem> { item }
                });

                // Do not dispose stream.
                _fileExists = true;
            }
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing && Stream != null && !_fileExists)
            {
                Stream?.Dispose();
                Stream = null;
            }
        }
    }
}
