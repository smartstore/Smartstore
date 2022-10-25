namespace Smartstore.Web.Api.Models.OData.Media
{
    public partial class MediaFileOperationResult
    {
        public int DestinationFileId { get; set; }
        public bool IsDuplicate { get; set; }
        public string UniquePath { get; set; }

        // Does not work. Never serialized no matter what you set.
        //[AutoExpand]
        //[ForeignKey("DestinationFileId")]
        //public FileItemInfo DestinationFile { get; set; }
    }
}
