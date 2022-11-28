namespace Smartstore.Web.Api.Models.Media
{
    /// <summary>
    /// Represents a folder of media files.
    /// </summary>
    public partial class FolderNodeInfo
    {
        /// <summary>
        /// The MediaFolder identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The identifier of the parent node (if any).
        /// </summary>
        public int? ParentId { get; set; }

        /// <summary>
        /// A value indicating whether the folder is a root album node.
        /// </summary>
        public bool IsAlbum { get; set; }

        /// <summary>
        /// The root album name.
        /// </summary>
        public string AlbumName { get; set; }

        /// <summary>
        /// The media album display order. 0 if the folder is not an album.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// The folder name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The path of the folder.
        /// </summary>
        /// <example>content/my-folder</example>
        public string Path { get; set; }

        public string Slug { get; set; }

        /// <summary>
        /// The current number of files. Unused, always 0 at the moment.
        /// </summary>
        public int FilesCount { get; set; }

        /// <summary>
        /// A value indicating whether the folder has subfolders.
        /// </summary>
        public bool HasChildren { get; set; }
    }
}
