using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using Smartstore.Core.Content.Media.Storage;
using Smartstore.IO;

namespace Smartstore.Core.Content.Media
{
    public partial class MediaFileInfo : IFile, ICloneable<MediaFileInfo>
    {
        private string _alt;
        private string _title;

        private readonly IMediaService _mediaService;
        private readonly IMediaStorageProvider _storageProvider;
        private readonly IMediaUrlGenerator _urlGenerator;

        public MediaFileInfo(MediaFile file, IMediaService mediaService, IMediaUrlGenerator urlGenerator, string directory)
        {
            _mediaService = mediaService;
            _storageProvider = mediaService.StorageProvider;
            _urlGenerator = urlGenerator;

            Initialize(file, directory);
        }

        private void Initialize(MediaFile file, string directory)
        {
            File = file;
            Directory = directory.EmptyNull();
            Path = Directory.Length > 0 ? Directory + '/' + Name : Name;

            if (file.Width != null && file.Height != null)
            {
                Size = new Size(file.Width.Value, file.Height.Value);
            }

            _cachedUrls.Clear();
        }

        #region Clone

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        object ICloneable.Clone()
            => Clone();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MediaFileInfo Clone()
        {
            var clone = new MediaFileInfo(File, _mediaService, _urlGenerator, Directory)
            {
                ThumbSize = this.ThumbSize,
                _alt = this._alt,
                _title = this._title
            };

            return clone;
        }

        #endregion

        [JsonIgnore]
        public MediaFile File { get; private set; }

        [JsonProperty("id")]
        public int Id => File.Id;

        [JsonProperty("folderId", NullValueHandling = NullValueHandling.Ignore)]
        public int? FolderId => File.FolderId;

        [JsonProperty("mime", NullValueHandling = NullValueHandling.Ignore)]
        public string MimeType => File.MimeType;

        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string MediaType => File.MediaType;

        [JsonProperty("isTransient", DefaultValueHandling = DefaultValueHandling.Ignore), DefaultValue(false)]
        public bool IsTransient => File.IsTransient;

        [JsonProperty("deleted", DefaultValueHandling = DefaultValueHandling.Ignore), DefaultValue(false)]
        public bool Deleted => File.Deleted;

        [JsonProperty("hidden", DefaultValueHandling = DefaultValueHandling.Ignore), DefaultValue(false)]
        public bool Hidden => File.Hidden;

        [JsonProperty("createdOn")]
        public DateTimeOffset CreatedOn => File.CreatedOnUtc;

        [JsonProperty("alt", NullValueHandling = NullValueHandling.Ignore)]
        public string Alt
        {
            get => _alt ?? File.Alt;
            set => _alt = value;
        }

        [JsonProperty("titleAttr", NullValueHandling = NullValueHandling.Ignore)]
        public string TitleAttribute
        {
            get => _title ?? File.Title;
            set => _title = value;
        }

        public static explicit operator MediaFile(MediaFileInfo fileInfo)
            => fileInfo.File;

        [JsonProperty("path", NullValueHandling = NullValueHandling.Ignore)]
        public string Path { get; private set; }

        #region Url

        private readonly Dictionary<(int size, string host), string> _cachedUrls = new();

        [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
        internal string Url => GetUrl(0, string.Empty);

        [JsonProperty("thumbUrl", NullValueHandling = NullValueHandling.Ignore)]
        internal string ThumbUrl => GetUrl(ThumbSize, string.Empty);

        [JsonIgnore]
        internal int ThumbSize
        {
            // For serialization of "ThumbUrl" in MediaManager
            get; set;
        } = 256;

        public string GetUrl(int maxSize = 0, string host = null)
        {
            var cacheKey = (maxSize, host);
            if (!_cachedUrls.TryGetValue(cacheKey, out var url))
            {
                var query = maxSize > 0
                    ? QueryString.Create("size", maxSize.ToString(CultureInfo.InvariantCulture))
                    : QueryString.Empty;
                
                url = _urlGenerator.GenerateUrl(this, query, host, false);

                _cachedUrls[cacheKey] = url;
            }

            return url;
        }

        #endregion

        #region IFile

        /// <inheritdoc/>
        [JsonIgnore]
        public bool Exists => File.Id > 0;

        /// <inheritdoc/>
        [JsonIgnore]
        bool IFileInfo.IsDirectory => false;

        /// <inheritdoc/>
        [JsonProperty("lastUpdated")]
        public DateTimeOffset LastModified => File.UpdatedOnUtc;

        /// <inheritdoc/>
        [JsonProperty("size")]
        public long Length => File.Size;

        /// <inheritdoc/>
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name => File.Name;

        /// <inheritdoc/>
        [JsonIgnore]
        string IFileInfo.PhysicalPath => Path;

        /// <inheritdoc/>
        Stream IFileInfo.CreateReadStream()
            => OpenRead();


        /// <inheritdoc/>
        [JsonIgnore]
        IFileSystem IFileEntry.FileSystem => throw new NotSupportedException();

        /// <inheritdoc/>
        [JsonIgnore]
        string IFileEntry.SubPath => Path;

        /// <inheritdoc/>
        bool IFileEntry.IsSymbolicLink(out string finalPhysicalPath)
        {
            finalPhysicalPath = null;
            return false;
        }


        /// <inheritdoc/>
        [JsonProperty("dir")]
        public string Directory { get; private set; }

        [JsonProperty("title")]
        public string NameWithoutExtension => System.IO.Path.GetFileNameWithoutExtension(Name);

        [JsonProperty("ext")]
        public string Extension => File.Extension != null ? "." + File.Extension : null;

        [JsonProperty("dimensions")]
        public Size Size { get; private set; }

        /// <inheritdoc/>
        public Stream OpenRead()
        {
            if (Exists)
            {
                return _storageProvider.OpenRead(File);
            }

            throw new FileNotFoundException(Path);
        }

        /// <inheritdoc/>
        public Task<Stream> OpenReadAsync()
        {
            if (Exists)
            {
                return _storageProvider.OpenReadAsync(File);
            }

            throw new FileNotFoundException(Path);
        }

        /// <inheritdoc/>
        void IFileEntry.Delete()
            => ((IFile)this).DeleteAsync().Await();

        /// <inheritdoc/>
        async Task IFileEntry.DeleteAsync()
        {
            if (!Exists)
            {
                throw new FileSystemException($"The file '{Path}' does not exist.");
            }

            await _mediaService.DeleteFileAsync(File, true);
            Initialize(File, Directory);
        }

        /// <inheritdoc/>
        IFile IFile.CopyTo(string newPath, bool overwrite)
            => ((IFile)this).CopyToAsync(newPath, overwrite).Await();

        /// <inheritdoc/>
        async Task<IFile> IFile.CopyToAsync(string newPath, bool overwrite)
        {
            if (!Exists)
            {
                throw new FileNotFoundException($"The file '{Path}' does not exist.");
            }

            var result = await _mediaService.CopyFileAsync(this, newPath, overwrite ? DuplicateFileHandling.Overwrite : DuplicateFileHandling.ThrowError);
            return result.DestinationFile;
        }

        /// <inheritdoc/>
        void IFileEntry.MoveTo(string newPath)
            => ((IFile)this).MoveToAsync(newPath).Await();

        /// <inheritdoc/>
        async Task IFileEntry.MoveToAsync(string newPath)
        {
            if (!Exists)
            {
                throw new FileNotFoundException($"The file '{Path}' does not exist.");
            }

            var file = await _mediaService.MoveFileAsync(File, newPath, DuplicateFileHandling.ThrowError);
            Initialize(file.File, file.Directory);
        }

        /// <inheritdoc/>
        void IFile.Create(Stream inStream, bool overwrite)
            => ((IFile)this).CreateAsync(inStream, overwrite).Await();

        /// <inheritdoc/>
        async Task IFile.CreateAsync(Stream inStream, bool overwrite)
        {
            var file = await _mediaService.SaveFileAsync(Path, inStream, false, overwrite ? DuplicateFileHandling.Overwrite : DuplicateFileHandling.ThrowError);
            Initialize(file.File, file.Directory);
        }

        #endregion
    }
}