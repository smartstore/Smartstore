using System.ComponentModel.DataAnnotations;
using Smartstore.Domain;

namespace Smartstore.Core.Seo
{
    /// <summary>
    /// Represents an URL record
    /// </summary>
    public partial class UrlRecord : BaseEntity
    {
        /// <summary>
        /// Gets or sets the entity identifier
        /// </summary>
        public int EntityId { get; set; }

        /// <summary>
        /// Gets or sets the entity name
        /// </summary>
        [Required, StringLength(400)]
        public string EntityName { get; set; }

        /// <summary>
        /// Gets or sets the slug
        /// </summary>
        [Required, StringLength(400)]
        public string Slug { get; set; }

        /// <summary>
	    /// Gets or sets the value indicating whether the record is active
	    /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the language identifier
        /// </summary>
        public int LanguageId { get; set; }
    }
}
