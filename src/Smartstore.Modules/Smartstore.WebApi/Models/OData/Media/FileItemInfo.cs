using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.OData.ModelBuilder;
using Smartstore.Core.Content.Media;
using Smartstore.Domain;

namespace Smartstore.Web.Api.Models.Media
{
    // INFO: should not be inherited from MediaFile because then navigation properties cannot be expanded using $expand (e.g. $expand=File($expand=Tracks)).
    // Should not use [Contained] because throws "The Path property in ODataMessageWriterSettings.ODataUri must be set when writing contained elements".
    // 
    // AutoExpand only works with <see cref="ApiQueryableAttribute" />.
    // Only exapnds to depth of 2. Bug: https://github.com/OData/WebApi/issues/1065

    /// <summary>
    /// Represents a media file. Acts as a wrapper for the underlying MediaFile entity.
    /// </summary>
    public partial class FileItemInfo : BaseEntity
    {
        /// <summary>
        /// The relative path without the file part (but with trailing slash).
        /// </summary>
        /// <example>catalog</example>
        public string Directory { get; set; }

        /// <summary>
        /// The path of the file.
        /// </summary>
        /// <example>content/my-file.jpg</example>
        public string Path { get; set; }

        /// <summary>
        /// The URL of the file.
        /// </summary>
        /// <example>media/40/catalog/my-picture.jpg</example>
        public string Url { get; set; }

        /// <summary>
        /// The thumbnail URL of the file.
        /// </summary>
        /// <example>media/40/catalog/my-picture.jpg?size=256</example>
        public string ThumbUrl { get; set; }

        /// <summary>
        /// The underlying MediaFile entity.
        /// </summary>
        [AutoExpand]
        [ForeignKey("Id")]
        public MediaFile File { get; set; }
    }
}
