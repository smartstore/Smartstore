#nullable enable

namespace Smartstore.Core.Common.Services
{
    public partial interface ICollectionGroupService
    {
        /// <summary>
        /// Applies a new collection group name to <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">The entity to apply the new collection group name to.</param>
        /// <param name="collectionGroupName">The new collection group name to apply.</param>
        /// <returns><c>true</c> <see cref="IGroupedEntity.CollectionGroupId"/> has been updated, otherwise <c>false</c>.</returns>
        Task<bool> ApplyCollectionGroupNameAsync<TEntity>(TEntity entity, string? collectionGroupName)
            where TEntity : BaseEntity, IGroupedEntity;
    }
}
