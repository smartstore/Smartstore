namespace Smartstore.Web.Api.Models.Media
{
    /// <summary>
    /// Represents the result of an order deletion.
    /// </summary>
    public partial class MediaFolderDeleteResult
    {
        /// <summary>
        /// Collection of identifiers of deleted folders.
        /// </summary>
        public ICollection<int> DeletedFolderIds { get; set; }

        /// <summary>
        /// Collection of names of deleted folders.
        /// </summary>
        public ICollection<string> DeletedFileNames { get; set; }
    }
}
