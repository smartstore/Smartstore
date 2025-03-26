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
        public EntityPatch(string entityName, int entityId)
        {
            Guard.NotEmpty(entityName);
            Guard.IsPositive(entityId);

            EntityName = entityName;
            EntityId = entityId;
        }

        /// <summary>
        /// Gets or sets the name of the entity type to patch.
        /// </summary>
        /// <value>
        /// The CLR type name or DbSet property name representing the entity type.
        /// </value>
        public string EntityName { get; }

        /// <summary>
        /// Gets or sets the primary key value of the entity to patch.
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
