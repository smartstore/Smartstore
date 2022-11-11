namespace Smartstore.Web.Api.Models.Media
{
    /// <summary>
    /// Represents the result of counting files.
    /// </summary>
    public partial class MediaCountResult
    {
        /// <summary>
        /// The total number of files.
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// The number of files in trash.
        /// </summary>
        public int Trash { get; set; }

        /// <summary>
        /// The number of unassigned files.
        /// </summary>
        public int Unassigned { get; set; }

        /// <summary>
        /// The number of transient/preliminary files.
        /// </summary>
        public int Transient { get; set; }

        /// <summary>
        /// The number of orphaned files.
        /// </summary>
        public int Orphan { get; set; }

        /// <summary>
        /// The number of files by folder.
        /// </summary>
        public ICollection<FolderCount> Folders { get; set; }

        public partial class FolderCount
        {
            /// <summary>
            /// The folder identifier.
            /// </summary>
            public int FolderId { get; set; }

            /// <summary>
            /// The number of files.
            /// </summary>
            public int Count { get; set; }
        }
    }
}
