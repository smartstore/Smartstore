namespace Smartstore.Web.Api.Models.OData.Media
{
    public partial class MediaCountResult
    {
        public int Total { get; set; }
        public int Trash { get; set; }
        public int Unassigned { get; set; }
        public int Transient { get; set; }
        public int Orphan { get; set; }
        public ICollection<FolderCount> Folders { get; set; }

        public partial class FolderCount
        {
            public int FolderId { get; set; }
            public int Count { get; set; }
        }
    }
}
