namespace Smartstore.Core.Common
{
    /// <summary>
    /// An event that is published immediately before the final deletion of ISoftDeletable entities.
    /// The event is usually part of deleting recycle bin items.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to be deleted.</typeparam>
    public class PermanentDeletionRequestedEvent<TEntity>
        where TEntity : BaseEntity, ISoftDeletable
    {
        public PermanentDeletionRequestedEvent(int[] entityIds)
        {
            Guard.IsTrue(!entityIds.IsNullOrEmpty());

            EntityType = typeof(TEntity);
            EntityIds = entityIds;
        }

        /// <summary>
        /// Gets the type of the entities to be deleted.
        /// </summary>
        public Type EntityType { get; }

        /// <summary>
        /// Gets the identifiers of the entities to be deleted.
        /// </summary>
        public int[] EntityIds { get; }

        /// <summary>
        /// Adds errors, e.g. reasons why entities cannot be deleted.
        /// </summary>
        public HashSet<string> Errors { get; set; } = new();

        /// <summary>
        /// Identifiers of entities that must not be deleted.
        /// </summary>
        public HashSet<int> DisallowedEntityIds { get; } = new();

        internal List<Func<CancellationToken, Task>> EntitiesDeletedCallbacks { get; } = new();

        /// <summary>
        /// Adds a callback that is called after the entities specified by <see cref="EntityIds"/> were deleted physically.
        /// </summary>
        public void AddEntitiesDeletedCallback(Func<CancellationToken, Task> entitiesDeleted)
        {
            if (entitiesDeleted != null)
            {
                EntitiesDeletedCallbacks.Add(entitiesDeleted);
            }
        }
    }

    public partial class DeletionResult
    {
        public int DeletedRecords { get; set; }

        public int SkippedRecords { get; set; }

        public IList<string> Errors { get; } = new List<string>();
    }
}
