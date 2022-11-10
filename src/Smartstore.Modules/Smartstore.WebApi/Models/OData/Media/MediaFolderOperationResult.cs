namespace Smartstore.Web.Api.Models.Media
{
    public partial class MediaFolderOperationResult
    {
        public int FolderId { get; set; }
        public FolderNodeInfo Folder { get; set; }
        public ICollection<DuplicateFileInfo> DuplicateFiles { get; set; }

        public partial class DuplicateFileInfo
        {
            public int SourceFileId { get; set; }

            public int DestinationFileId { get; set; }

            //public FileItemInfo SourceFile { get; set; }

            //public FileItemInfo DestinationFile { get; set; }

            public string UniquePath { get; set; }
        }
    }
}
