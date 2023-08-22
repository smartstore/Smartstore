namespace Smartstore.Core.Common
{
    /// <summary>
    /// An event that is published immediately before the final deletion of ISoftDeletable entities.
    /// The event is usually part of delteing recycle bin items.
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

        // TODO: (mg) Bad architecture: A plugin must not prevent the deletion of the whole batch!
        // Only particular entities. This requires revision (e.g. HashSet of undeletable ids etc.).
        /// <summary>
        /// Gets or sets a value indicating whether the deletion should be executed.
        /// If this is set to <c>true</c> by any event consumer, then none of the entities specified by <see cref="EntityIds"/> will be deleted.
        /// </summary>
        public bool Disallow { get; set; }

        /// <summary>
        /// Gets or sets an optional message why the deletion is disallowed.
        /// </summary>
        public string DisallowMessage { get; set; }

        // TODO: (mg) Mem leak risk: After deletion completes, this list should be cleared.
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
}
