namespace Smartstore.Web.Api.Models.Media
{
    /// <summary>
    /// Represents a file operation.
    /// </summary>
    public partial class MediaFileOperationResult
    {
        /// <summary>
        /// The identifier of the destination file.
        /// </summary>
        public int DestinationFileId { get; set; }

        /// <summary>
        /// A value indicating whether the file is a duplicate.
        /// </summary>
        public bool IsDuplicate { get; set; }

        /// <summary>
        /// The full path of the file. null if the file is a duplicate.
        /// </summary>
        public string UniquePath { get; set; }

        // Does not work. Never serialized no matter what you set.
        //[AutoExpand]
        //[ForeignKey("DestinationFileId")]
        //public FileItemInfo DestinationFile { get; set; }
    }
}
