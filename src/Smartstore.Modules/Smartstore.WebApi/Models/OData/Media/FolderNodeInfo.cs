using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.OData.ModelBuilder;
using Smartstore.Domain;

namespace Smartstore.Web.Api.Models.Media
{
    // INFO: AutoExpand only works with <see cref="ApiQueryableAttribute" />.
    // Only exapnds to depth of 2. Bug: https://github.com/OData/WebApi/issues/1065

    /// <summary>
    /// Represents a folder of media files.
    /// </summary>
    public partial class FolderNodeInfo : BaseEntity
    {
        /// <summary>
        /// The identifier of the parent node (if any).
        /// </summary>
        public int? ParentId { get; set; }

        /// <summary>
        /// The root album name.
        /// </summary>
        public string AlbumName { get; set; }

        /// <summary>
        /// The folder name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A value indicating whether the folder is a root album node.
        /// </summary>
        public bool IsAlbum { get; set; }

        /// <summary>
        /// The path of the folder.
        /// </summary>
        /// <example>content/my-folder</example>
        public string Path { get; set; }

        public string Slug { get; set; }

        /// <summary>
        /// A value indicating whether the folder has subfolders.
        /// </summary>
        public bool HasChildren { get; set; }

        /// <summary>
        /// Collection of subfolders (if any).
        /// </summary>
        [AutoExpand]
        [ForeignKey("Id")]
        public ICollection<FolderNodeInfo> Children { get; set; }
    }
}
