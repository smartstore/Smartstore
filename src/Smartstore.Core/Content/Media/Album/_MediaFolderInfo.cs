using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Smartstore.Collections;
using Smartstore.IO;

namespace Smartstore.Core.Content.Media
{
    public partial class MediaFolderInfo //: IDirectory
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

        #region IDirectory

        //[JsonIgnore]
        //bool IDirectory.IsRoot => false;

        //[JsonIgnore]
        //bool IDirectory.FileSystem => null;

        [JsonProperty("path")]
        public string Path => Node.Value.Path;

        [JsonProperty("name")]
        public string Name => Node.Value.Name;

        [JsonIgnore]
        public long Length => 0;

        [JsonIgnore]
        public bool Exists => Node.Value.Id > 0;

        [JsonIgnore]
        public DateTime LastUpdated => DateTime.UtcNow;

        //[JsonIgnore]
        //public IDirectory Parent => Node.Parent == null ? null : new MediaFolderInfo(Node.Parent);

        #endregion
    }
}
