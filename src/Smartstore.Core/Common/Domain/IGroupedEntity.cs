namespace Smartstore.Core.Common
{
    /// <summary>
    /// Represents an entity that can be grouped by name.
    /// </summary>
    public interface IGroupedEntity
    {
        /// <summary>
        /// Gets or sets the identifier of an optional <see cref="CollectionGroupMapping"/>.
        /// </summary>
        int? CollectionGroupMappingId { get; set; }

        /// <summary>
        /// Gets or sets the mapping to a <see cref="CollectionGroup"/>.
        /// </summary>
        CollectionGroupMapping CollectionGroupMapping { get; set; }
    }
}
