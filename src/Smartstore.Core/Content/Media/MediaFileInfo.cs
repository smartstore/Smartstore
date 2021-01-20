using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using Smartstore.Core.Content.Media.Imaging;
using Smartstore.Core.Content.Media.Storage;
using Smartstore.IO;

namespace Smartstore.Core.Content.Media
{
    public partial class MediaFileInfo : IFile, ICloneable<MediaFileInfo>
    {
        private string _alt;
        private string _title;

        private readonly IMediaStorageProvider _storageProvider;
        private readonly IMediaUrlGenerator _urlGenerator;

        public MediaFileInfo(MediaFile file, IMediaStorageProvider storageProvider, IMediaUrlGenerator urlGenerator, string directory)
        {
            File = file;
            Directory = directory.EmptyNull();
            Path = Directory.Length > 0
                ? Directory + '/' + file.Name
                : file.Name;

            if (file.Width.HasValue && file.Height.HasValue)
            {
                Size = new Size(file.Width.Value, file.Height.Value);
            }

            _storageProvider = storageProvider;
            _urlGenerator = urlGenerator;
        }

        #region Clone

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        object ICloneable.Clone()
            => Clone();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MediaFileInfo Clone()
        {
            var clone = new MediaFileInfo(File, _storageProvider, _urlGenerator, Directory)
            {
                ThumbSize = this.ThumbSize,
                _alt = this._alt,
                _title = this._title
            };

            return clone;
        }

        #endregion

        [JsonIgnore]
        public MediaFile File { get; }

        [JsonProperty("id")]
        public int Id => File.Id;

        [JsonProperty("folderId")]
        public int? FolderId => File.FolderId;

        [JsonProperty("mime")]
        public string MimeType => File.MimeType;

        [JsonProperty("type")]
        public string MediaType => File.MediaType;

        [JsonProperty("isTransient")]
        public bool IsTransient => File.IsTransient;

        [JsonProperty("deleted")]
        public bool Deleted => File.Deleted;

        [JsonProperty("hidden")]
        public bool Hidden => File.Hidden;

        [JsonProperty("createdOn")]
        public DateTime CreatedOn => File.CreatedOnUtc;

        [JsonProperty("alt")]
        public string Alt
        {
            get => _alt ?? File.Alt;
            set => _alt = value;
        }

        [JsonProperty("titleAttr")]
        public string TitleAttribute
        {
            get => _title ?? File.Title;
            set => _title = value;
        }

        public static explicit operator MediaFile(MediaFileInfo fileInfo)
            => fileInfo.File;

        [JsonProperty("path")]
        public string Path { get; }

        #region Url

        private readonly Dictionary<(int size, string host), string> _cachedUrls = new();

        [JsonProperty("url")]
        internal string Url => GetUrl(0, string.Empty);

        [JsonProperty("thumbUrl")]
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
        bool IFileInfo.Exists => File?.Id > 0;

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
        [JsonProperty("name")]
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
        public string Directory { get; }

        [JsonProperty("title")]
        public string NameWithoutExtension => System.IO.Path.GetFileNameWithoutExtension(File.Name);

        [JsonProperty("ext")]
        public string Extension => "." + File.Extension;

        [JsonProperty("dimensions")]
        public Size Size { get; }

        public Stream OpenRead()
            => _storageProvider?.OpenRead(File);

        public Task<Stream> OpenReadAsync()
            => _storageProvider?.OpenReadAsync(File);

        public Stream OpenWrite()
            => throw new NotSupportedException();

        #endregion
    }
}