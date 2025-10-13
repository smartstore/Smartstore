using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Catalog
{
    public partial class ProductSpecificationModel : ModelBase
    {
        /// <summary>
        /// Gets or sets a value indicating whether the attribute is assigned to a collection group.
        /// </summary>
        public bool IsGrouped { get; set; }

        /// <summary>
        /// Gets or sets the name of the collection group.
        /// <c>null</c> if the attribute is not assigned to a collection group.
        /// </summary>
        public string CollectionGroupName { get; set; }

        /// <summary>
        /// Gets or sets the display order of the collection group.
        /// <c>0</c> if the attribute is not assigned to a collection group.
        /// </summary>
        public int CollectionGroupDisplayOrder { get; set; }

        public int SpecificationAttributeId { get; set; }
        public LocalizedValue<string> SpecificationAttributeName { get; set; }
        public LocalizedValue<string> SpecificationAttributeOption { get; set; }
        public bool Essential { get; set; }
        public int DisplayOrder { get; set; }
    }
}
