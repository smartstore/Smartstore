using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;

namespace Smartstore
{
    public static class IGenericAttributeServiceExtensions
    {
        /// <summary>
        /// Gets a specialized generic attributes collection for the given entity.
        /// Loaded data will be cached for the duration of the request.
        /// </summary>
        /// <param name="entity">The entity instance to get attributes for.</param>
        public static GenericAttributeCollection GetAttributesForEntity(this IGenericAttributeService service, BaseEntity entity)
        {
            Guard.NotNull(service, nameof(service));
            Guard.NotNull(entity, nameof(entity));

            return service.GetAttributesForEntity(entity.GetEntityName(), entity.Id);
        }

        /// <summary>
        /// Prefetches a collection of generic attributes for a range of entities in one go
        /// and caches them for the duration of the current request.
        /// </summary>
        /// <param name="entities">The entity instances to prefetch attributes for.</param>
        public static async Task PrefetchAttributesAsync(this IGenericAttributeService service, params BaseEntity[] entities)
        {
            Guard.NotNull(service, nameof(service));

            var groupedEntities = entities.GroupBy(x => x.GetEntityName());

            foreach (var group in groupedEntities)
            {
                await service.PrefetchAttributesAsync(group.Key, group.Select(x => x.Id).ToArray());
            }
        }
    }
}