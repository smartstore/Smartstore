#nullable enable

using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Smartstore.Data;
using Smartstore.Domain;

namespace Smartstore
{
    public static partial class DbContextExtensions
    {
        /// <inheritdoc cref="PatchEntityInternal(DbContext, EntityPatch, bool, CancellationToken)"/>/>
        public static BaseEntity PatchEntity(this DbContext db, EntityPatch patch)
            => PatchEntityInternal(db, patch, false).Await();

        /// <inheritdoc cref="PatchEntityInternal(DbContext, EntityPatch, bool, CancellationToken)"/>/>
        public static Task<BaseEntity> PatchEntityAsync(this DbContext db, EntityPatch patch, CancellationToken cancelToken = default)
            => PatchEntityInternal(db, patch, true, cancelToken);

        /// <summary>
        /// Applies an untyped patch to an entity in the context.
        /// </summary>
        /// <param name="db">The DbContext instance being extended.</param>
        /// <param name="patch">The patch operation containing entity identification and property updates.</param>
        /// <param name="cancelToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>The patched entity.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="db"/> or <paramref name="patch"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when:
        /// - The entity type cannot be found
        /// - The entity doesn't have a single auto-increment primary key
        /// - The entity instance cannot be found
        /// - A specified property doesn't exist
        /// - Type conversion fails for a property value
        /// </exception>
        /// <remarks>
        /// <para>
        /// This method performs a partial update (PATCH) operation on an entity identified by its primary key.
        /// It only updates the properties explicitly specified in the patch, leaving other properties unchanged.
        /// </para>
        /// <para>
        /// The method handles:
        /// - Entity type resolution by name
        /// - Property name validation
        /// - Automatic type conversion for property values
        /// - Navigation property filtering (skips navigation properties)
        /// - Change tracking
        /// </para>
        /// <para>
        /// Note: This method will NOT save changes to the database.
        /// </para>
        /// </remarks>
        public static async Task<BaseEntity> PatchEntityInternal(this DbContext db, EntityPatch patch, bool async, CancellationToken cancelToken = default)
        {
            Guard.NotNull(db);
            Guard.NotNull(patch);

            var entityName = patch.EntityName ?? patch.EntityType?.FullName;

            // Resolve entity type from model
            var entityType = patch.EntityType == null ? db.Model.FindEntityType(patch.EntityName!) : db.Model.FindEntityType(patch.EntityType);
            if (entityType == null)
            {
                throw new InvalidOperationException($"Entity type '{entityName}' not found in the model.");
            }

            // Validate primary key configuration
            var primaryKey = entityType.FindPrimaryKey();
            if (primaryKey == null || primaryKey.Properties.Count != 1)
            {
                throw new InvalidOperationException($"Entity type '{entityName}' must have a single primary key property.");
            }

            var primaryKeyProperty = primaryKey.Properties[0];
            if (primaryKeyProperty.Name != nameof(BaseEntity.Id) || primaryKeyProperty.ValueGenerated != ValueGenerated.OnAdd)
            {
                throw new InvalidOperationException($"Primary key of entity type '{entityName}' must be auto-incrementing.");
            }

            // Load the existing entity
            var entity = async 
                ? await db.FindAsync(entityType.ClrType, [patch.EntityId], cancelToken) as BaseEntity
                : db.Find(entityType.ClrType, [patch.EntityId]) as BaseEntity;
            if (entity == null)
            {
                throw new InvalidOperationException($"Entity of type '{entityName}' with ID {patch.EntityId} not found.");
            }

            // Apply property updates
            foreach (var (propertyName, value) in patch.Properties)
            {
                var property = entityType.FindProperty(propertyName);
                if (property == null)
                {
                    throw new InvalidOperationException($"Property '{propertyName}' not found on entity type '{entityName}'.");
                }

                // Skip navigation properties and foreign keys
                if (property.IsForeignKey() || entityType.FindNavigation(propertyName) != null)
                {
                    continue;
                }

                // Set property value with type conversion
                var propertyInfo = entityType.ClrType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (propertyInfo == null)
                {
                    throw new InvalidOperationException($"Property '{propertyName}' not found on type '{entityType.ClrType.Name}'.");
                }

                object? convertedValue = value;
                if (value != null && propertyInfo.PropertyType != value.GetType())
                {
                    convertedValue = value.Convert(propertyInfo.PropertyType);
                }

                propertyInfo.SetValue(entity, convertedValue);
            }

            return entity;
        }
    }
}
