using System;
using System.IO;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using Smartstore.Collections;
using Smartstore.IO;

namespace Smartstore.Core.Content.Media
{
    public partial class MediaFolderInfo : IDirectory
    {
        public MediaFolderInfo(TreeNode<MediaFolderNode> node)
        {
            Node = node;
        }

        [JsonIgnore]
        public TreeNode<MediaFolderNode> Node { get; }

        [JsonProperty("filesCount")]
        public int FilesCount => Node.Value.FilesCount;

        public static implicit operator TreeNode<MediaFolderNode>(MediaFolderInfo folderInfo)
        {
            return folderInfo.Node;
        }

        [JsonProperty("id")]
        public int Id => Node.Value.Id;

        [JsonProperty("path")]
        public string Path => Node.Value.Path;

        #region IDirectory

        /// <inheritdoc/>
        [JsonIgnore]
        bool IFileInfo.Exists => Node.Value.Id > 0;

        /// <inheritdoc/>
        [JsonIgnore]
        bool IFileInfo.IsDirectory => true;

        /// <inheritdoc/>
        [JsonIgnore]
        DateTimeOffset IFileInfo.LastModified => DateTime.UtcNow;

        /// <inheritdoc/>
        [JsonIgnore]
        long IFileInfo.Length => -1;

        /// <inheritdoc/>
        [JsonProperty("name")]
        public string Name => Node.Value.Name;

        /// <inheritdoc/>
        [JsonIgnore]
        string IFileInfo.PhysicalPath => Node.Value.Path;

        /// <inheritdoc/>
        Stream IFileInfo.CreateReadStream() => throw new NotSupportedException();


        /// <inheritdoc/>
        [JsonIgnore]
        IFileSystem IFileEntry.FileSystem => throw new NotSupportedException();

        /// <inheritdoc/>
        [JsonIgnore]
        string IFileEntry.SubPath => Node.Value.Path;

        /// <inheritdoc/>
        bool IFileEntry.IsSymbolicLink(out string finalPhysicalPath)
        {
            finalPhysicalPath = null;
            return false;
        }


        /// <inheritdoc/>
        [JsonIgnore]
        bool IDirectory.IsRoot => false;

        /// <inheritdoc/>
        [JsonIgnore]
        public IDirectory Parent => Node.Parent == null ? null : new MediaFolderInfo(Node.Parent);

        #endregion
    }
}
