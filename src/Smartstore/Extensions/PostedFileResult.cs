using Microsoft.AspNetCore.Http;
using Smartstore;
using Smartstore.IO;
using System;
using System.IO;
using System.Text.RegularExpressions;

// TODO: (ms) (core) Either remove it if discussed so with MC or fix namespace
namespace SmartStore
{
    public class PostedFileResult
    {
        private static readonly Regex s_ImageTypes = new Regex(@"(.*?)\.(gif|jpg|jpeg|jpe|jfif|pjpeg|pjp|png|tiff|tif|bmp|ico|svg)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private string _contentType;
        private string _fileName;
        private string _fileTitle;
        private string _fileExt;
        private bool? _isImage;
        private byte[] _buffer;

        public PostedFileResult(IFormFile httpFile)
        {
            Guard.NotNull(httpFile, nameof(httpFile));

            File = httpFile;

            TimeStamp = DateTime.UtcNow;
            BatchId = Guid.NewGuid();
        }

        public IFormFile File { get; init; }

        public DateTime TimeStamp
        {
            get;
            private set;
        }

        public Guid BatchId
        {
            get;
            internal set;
        }

        public string FileName => _fileName ??= Path.GetFileName(File.FileName);

        public string FileTitle => _fileTitle ??= Path.GetFileNameWithoutExtension(FileName);

        public string FileExtension => _fileExt ??= Path.GetExtension(FileName).EmptyNull();

        public string ContentType
        {
            get
            {
                if (_contentType == null)
                {
                    var contentType = File.ContentType;
                    if (contentType == null && FileExtension.HasValue())
                    {
                        contentType = MimeTypes.MapNameToMimeType(FileExtension);
                    }

                    _contentType = contentType.EmptyNull();
                }

                return _contentType;
            }
        }

        public bool IsImage => _isImage ??= s_ImageTypes.IsMatch(FileExtension);

        public long Size => File.Length;

        public Stream Stream => File.OpenReadStream();

        public byte[] Buffer => _buffer ??= File.OpenReadStream().ToByteArray();

        public bool FileNameMatches(string pattern)
        {
            return Regex.IsMatch(File.FileName, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }
    }
}