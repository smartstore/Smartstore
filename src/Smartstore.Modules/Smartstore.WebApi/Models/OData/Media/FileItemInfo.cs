using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Smartstore.Core.Content.Media;

namespace Smartstore.Web.Api.Models.Media
{
    // INFO: should not be inherited from MediaFile because then navigation properties cannot be expanded using $expand (e.g. $expand=File($expand=Tracks)).
    // Should not use [Contained] because throws "The Path property in ODataMessageWriterSettings.ODataUri must be set when writing contained elements".

    [DataContract]
    public partial class FileItemInfo
    {
        /// <summary>
        /// The MediaFile identifier.
        /// </summary>
        [DataMember]
        public int FileId { get; set; }

        /// <summary>
        /// The relative path without the file part (but with trailing slash).
        /// </summary>
        /// <example>catalog</example>
        [DataMember]
        public string Directory { get; set; }

        /// <summary>
        /// The path of the file.
        /// </summary>
        /// <example>content/my-file.jpg</example>
        [DataMember]
        public string Path { get; set; }

        /// <summary>
        /// The URL of the file.
        /// </summary>
        /// <example>media/40/catalog/my-picture.jpg</example>
        [DataMember]
        public string Url { get; set; }

        /// <summary>
        /// The thumbnail URL of the file.
        /// </summary>
        /// <example>media/40/catalog/my-picture.jpg?size=256</example>
        [DataMember]
        public string ThumbUrl { get; set; }

        // TODO: (mg) (core) still does not work. "File" is NEVER rendered...
        // probably a ODataModelBuilder\EDM issue -> finally stop this and inherit from MediaFile....

        /// <summary>
        /// The underlying MediaFile entity.
        /// </summary>
        [DataMember]
        [ForeignKey(nameof(FileId))]
        public MediaFile File { get; set; }
    }
}
