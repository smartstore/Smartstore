using Microsoft.OData.ModelBuilder;
using Smartstore.Domain;

namespace Smartstore.Web.Api.Models.OData.Media
{
    /// <summary>
    /// Represents a folder media files.
    /// </summary>
    public partial class FolderNodeInfo : BaseEntity
    {
        public int? ParentId { get; set; }

        /// <summary>
        /// The root album name.
        /// </summary>
        public string AlbumName { get; set; }

        /// <summary>
        /// Folder name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A value indicating whether the folder is a root album node.
        /// </summary>
        public bool IsAlbum { get; set; }

        public string Path { get; set; }

        public string Slug { get; set; }

        public bool HasChildren { get; set; }

        /// <remarks>
        /// AutoExpand only works with <see cref="WebApiQueryableAttribute" />.
        /// Only exapnds to depth of 2. Bug: https://github.com/OData/WebApi/issues/1065
        /// </remarks>
        [AutoExpand]
        public ICollection<FolderNodeInfo> Children { get; set; }
    }
}
