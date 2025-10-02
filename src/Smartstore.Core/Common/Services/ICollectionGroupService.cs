namespace Smartstore.Core.Common.Services
{
    public partial interface ICollectionGroupService
    {
        /// <summary>
        /// Updates collection groups for a given entity. This method commits to database.
        /// </summary>
        /// <param name="entityId">The identifier of the entity to which the collection groups belong.</param>
        /// <param name="entityName">The name of the entity to which the collection groups belong.</param>
        /// <param name="groupNames">The new group names.</param>
        Task UpdateCollectionGroupsAsync(int entityId, string entityName, IEnumerable<string> groupNames);
    }
}
