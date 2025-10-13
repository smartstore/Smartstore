using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Common
{
    /// <summary>
    /// Represents a group of entities, like a group of specification attribute options.
    /// </summary>
    [Index(nameof(Name))]
    [Index(nameof(EntityName))]
    [Index(nameof(DisplayOrder))]
    [LocalizedEntity(nameof(Published))]
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
        /// Gets or sets the name of the group.
        /// </summary>
        [Required, StringLength(400)]
        [LocalizedProperty]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the collection group is published.
        /// </summary>
        public bool Published { get; set; } = true;

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        public int DisplayOrder { get; set; }

        private ICollection<CollectionGroupMapping> _collectionGroupMappings;
        public ICollection<CollectionGroupMapping> CollectionGroupMappings
        {
            get => LazyLoader?.Load(this, ref _collectionGroupMappings) ?? (_collectionGroupMappings ??= []);
            protected set => _collectionGroupMappings = value;
        }
    }
}
