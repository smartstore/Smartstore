#nullable enable

namespace Smartstore.Core.Common.Services
{
    public partial interface ICollectionGroupService
    {
        /// <summary>
        /// Applies a collection group name to <paramref name="entity"/>.
        /// Adds a <see cref="CollectionGroup"/> if none exists for <paramref name="collectionGroupName"/>.
        /// The caller is responsible for database commit.
        /// </summary>
        /// <param name="entity">The entity to apply the new collection group name to.</param>
        /// <param name="collectionGroupName">The new collection group name to apply. Pass <c>null</c> to remove the assignment.</param>
        /// <returns><c>true</c> if the assignment to a collection group has changed, otherwise <c>false</c>.</returns>
        Task<bool> ApplyCollectionGroupNameAsync<TEntity>(TEntity entity, string? collectionGroupName)
            where TEntity : BaseEntity, IGroupedEntity;
    }
}
