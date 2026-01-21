using System.Text.Json.Serialization;
using Smartstore.Collections;

namespace Smartstore.Core.Content.Media
{
    public class MediaFolderNode : IKeyedNode
    {
        object IKeyedNode.GetNodeKey()
        {
            return Id;
        }

        /// <summary>
        /// Whether the folder is a root album node
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IsAlbum { get; set; }

        /// <summary>
        /// The root album name
        /// </summary>
        public string AlbumName { get; set; }

        /// <summary>
        /// Entity Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Folder name
        /// </summary>
        public string Name { get; set; }
        public string Path { get; set; }
        public string Slug { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool CanDetectTracks { get; set; }

        public int? ParentId { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int FilesCount { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string ResKey { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IncludePath { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Order { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Color { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string OverlayIcon { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string OverlayColor { get; set; }
    }
}