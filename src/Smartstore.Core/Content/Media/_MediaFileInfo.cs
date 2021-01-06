using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;

namespace Smartstore.Core.Content.Media
{
    public partial class MediaFileInfo : IFileInfo
    {
        public MediaFileInfo(MediaFile file/*, IMediaStorageProvider storageProvider, IMediaUrlGenerator urlGenerator*/, string directory)
        {
            File = file;
            Directory = directory.EmptyNull().TrimStart('/').EnsureEndsWith('/');
        }

        [JsonIgnore]
        public MediaFile File { get; }

        [JsonProperty("id")]
        public int Id => File.Id;

        [JsonProperty("path")]
        public string Path => (Directory + File.Name);

        #region IFile

        /// <inheritdoc/>
        [JsonIgnore]
        bool IFileInfo.Exists => File?.Id > 0;

        /// <inheritdoc/>
        [JsonIgnore]
        bool IFileInfo.IsDirectory => false;

        /// <inheritdoc/>
        [JsonProperty("lastUpdated")]
        DateTimeOffset IFileInfo.LastModified => File.UpdatedOnUtc;

        /// <inheritdoc/>
        [JsonProperty("size")]
        long IFileInfo.Length => File.Size;

        /// <inheritdoc/>
        [JsonProperty("name")]
        public string Name => File.Name;

        /// <inheritdoc/>
        [JsonIgnore]
        string IFileInfo.PhysicalPath => Path;

        /// <inheritdoc/>
        Stream IFileInfo.CreateReadStream() => throw new NotSupportedException();


        [JsonProperty("dir")]
        public string Directory { get; }

        #endregion
    }
}
