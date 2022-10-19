using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.OData.ModelBuilder;
using Smartstore.Core.Content.Media;
using Smartstore.Domain;

namespace Smartstore.Web.Api.Models.OData.Media
{
    // INFO: should not be inherited from MediaFile because then navigation properties cannot be expanded using $expand (e.g. $expand=File($expand=Tracks)).
    // Should not use [Contained] because throws "The Path property in ODataMessageWriterSettings.ODataUri must be set when writing contained elements".

    /// <summary>
    /// Represents a wrapper for the underlying MediaFile entity.
    /// </summary>
    public partial class FileItemInfo : BaseEntity
    {
        //public int Id { get; set; }

        public string Directory { get; set; }

        public string Path { get; set; }

        public string Url { get; set; }

        public string ThumbUrl { get; set; }

        /// <remarks>AutoExpand only works with <see cref="WebApiQueryableAttribute" />.</remarks>
        [AutoExpand]
        [ForeignKey("Id")]
        public MediaFile File { get; set; }
    }
}
