using System.ComponentModel.DataAnnotations;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Common
{
    /// <summary>
    /// Represents an entity that can be grouped by name.
    /// </summary>
    public interface IGroupedEntity
    {
        /// <summary>
        /// Gets or sets the identifier of an optional <see cref="CollectionGroup"/>.
        /// </summary>
        int? CollectionGroupId { get; set; }
    }

    /// <summary>
    /// Represents a group of entities, like a group of specification attributes.
    /// </summary>
    [Index(nameof(EntityName), nameof(EntityId))]
    [Index(nameof(Name))]
    [Index(nameof(DisplayOrder))]
    public partial class CollectionGroup : BaseEntity, ILocalizedEntity, IDisplayedEntity, IDisplayOrder
    {
        public string[] GetDisplayNameMemberNames() => [nameof(Name)];
        public string GetDisplayName() => Name;

        /// <summary>
        /// Gets or sets the entity name.
        /// </summary>
        [Required, StringLength(100)]
        public string EntityName { get; set; }

        /// <summary>
        /// Gets or sets the entity identifier in Smartstore.
        /// </summary>
        public int EntityId { get; set; }

        /// <summary>
        /// Gets or sets the name of the group.
        /// </summary>
        [Required, StringLength(400)]
        [LocalizedProperty]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the collection group is published.
        /// </summary>
        public bool Published { get; set; }

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        public int DisplayOrder { get; set; }
    }
}
