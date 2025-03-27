#nullable enable

namespace Smartstore.Data
{
    /// <summary>
    /// Represents a patch operation for an entity with untyped property values.
    /// </summary>
    /// <remarks>
    /// This class encapsulates all information needed to perform a partial update (PATCH)
    /// on an entity, including the entity type, identifier, and property values to update.
    /// </remarks>
    public record EntityPatch
    {
        public EntityPatch(Type entityType, int entityId)
        {
            Guard.NotNull(entityType);
            Guard.IsPositive(entityId);

            EntityType = entityType;
            EntityId = entityId;
        }

        public EntityPatch(string entityName, int entityId)
        {
            Guard.NotEmpty(entityName);
            Guard.IsPositive(entityId);

            EntityName = entityName;
            EntityId = entityId;
        }

        /// <summary>
        /// Gets the the entity type to patch.
        /// </summary>
        /// <value>
        /// The entity type.
        /// </value>
        public Type? EntityType { get; }

        /// <summary>
        /// Gets the name of the entity type to patch.
        /// </summary>
        /// <value>
        /// The conceptual name of the entity type (usually the type's full name without assembly part, e.g. "Smartstore.Core.Catalog.Products.Product")
        /// </value>
        public string? EntityName { get; }

        /// <summary>
        /// Gets the primary key value of the entity to patch.
        /// </summary>
        /// <value>
        /// The auto-incrementing primary key value that identifies the entity to update.
        /// </value>
        public int EntityId { get; }

        /// <summary>
        /// Gets the dictionary of property names and their corresponding values to update.
        /// </summary>
        /// <value>
        /// A dictionary where keys are property names and values are the new values to set.
        /// Null values are supported to represent null assignments.
        /// </value>
        public Dictionary<string, object?> Properties { get; set; } = [];
    }
}
