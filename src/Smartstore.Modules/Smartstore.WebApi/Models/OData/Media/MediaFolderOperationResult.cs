namespace Smartstore.Web.Api.Models.Media
{
    /// <summary>
    /// Represents a folder operation.
    /// </summary>
    public partial class MediaFolderOperationResult
    {
        /// <summary>
        /// The folder identifier.
        /// </summary>
        public int FolderId { get; set; }

        /// <summary>
        /// Folder info.
        /// </summary>
        public FolderNodeInfo Folder { get; set; }

        /// <summary>
        /// Collection of file duplicates.
        /// </summary>
        public ICollection<DuplicateFileInfo> DuplicateFiles { get; set; }

        public partial class DuplicateFileInfo
        {
            /// <summary>
            /// The identifier of the source file.
            /// </summary>
            public int SourceFileId { get; set; }

            /// <summary>
            /// The identifier of the destination file.
            /// </summary>
            public int DestinationFileId { get; set; }

            //public FileItemInfo SourceFile { get; set; }
            //public FileItemInfo DestinationFile { get; set; }

            /// <summary>
            /// The full path of the destination file.
            /// </summary>
            public string UniquePath { get; set; }
        }
    }
}
