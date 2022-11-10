namespace Smartstore.Web.Api.Models.Media
{
    public partial class MediaFolderDeleteResult
    {
        public ICollection<int> DeletedFolderIds { get; set; }
        public ICollection<string> DeletedFileNames { get; set; }
    }
}
