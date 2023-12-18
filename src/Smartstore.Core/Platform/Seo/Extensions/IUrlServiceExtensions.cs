using Smartstore.Threading;

namespace Smartstore.Core.Seo
{
    public static partial class IUrlServiceExtensions
    {
        /// <summary>
        /// Applies a slug without sanitization or uniqueness check. This method
        /// throws if the given slug already exists in the database. The recommended
        /// way to apply a slug is to call <see cref="IUrlService.ValidateSlugAsync{T}(T, string, string, bool, int?, bool)"/>
        /// first, then to call <see cref="IUrlService.ApplySlugAsync(ValidateSlugResult, bool)"/> by passing
        /// the return value from first call.
        /// </summary>
        /// <typeparam name="T">Type of slug supporting entity</typeparam>
        /// <param name="entity">Entity instance</param>
        /// <param name="slug">Slug to apply</param>
        /// <param name="languageId">Language ID</param>
        /// <param name="save"><c>true</c> will commit result to database.</param>
        /// <returns>
        /// The affected <see cref="UrlRecord"/> instance, either new or existing as tracked entity.
        /// </returns>
        public static Task<UrlRecord> ApplySlugAsync<T>(this IUrlService service, T entity, string slug, int languageId, bool save = false)
            where T : ISlugSupported
        {
            Guard.NotNull(entity);

            var input = new ValidateSlugResult
            {
                Source = entity,
                Slug = slug,
                LanguageId = languageId
            };

            return service.ApplySlugAsync(input, save);
        }

        /// <inheritdoc cref="IUrlService.ValidateSlugAsync{T}(T, string, string, bool, int?, bool)"/>
        /// <param name="seName">Search engine name to validate. If <c>null</c> or empty, the slug will be resolved from "<paramref name="entity"/>.GetDisplayName()".</param>
        public static ValueTask<ValidateSlugResult> ValidateSlugAsync<T>(this IUrlService service, T entity, string seName, bool ensureNotEmpty, int? languageId = null)
            where T : ISlugSupported
        {
            return service.ValidateSlugAsync(entity, seName, entity.GetDisplayName(), ensureNotEmpty, languageId);
        }

        /// <inheritdoc cref="SaveSlugAsync{T}(IUrlService, T, string, string, bool, int?, bool)"/>
        public static Task<UrlRecord> SaveSlugAsync<T>(this IUrlService service, T entity, string seName, bool ensureNotEmpty, int? languageId = null)
            where T : ISlugSupported
        {
            return SaveSlugAsync(service, entity, seName, entity.GetDisplayName(), ensureNotEmpty, languageId);
        }


        /// <summary>
        /// Validates, applies and saves a slug in a single atomic operation.
        /// </summary>
        /// A <see cref="UrlRecord"/> instance or null if the lock could not be acquired, e.g. due to timeout.
        /// <returns>
        /// </returns>
        /// <remarks>
        /// This method is thread-safe.
        /// Call it to prevent another parallel request to recreate the slug record.
        /// Lock acquisition timeout is 3 seconds.
        /// </remarks>
        /// <inheritdoc cref="IUrlService.ValidateSlugAsync{T}(T, string, string, bool, int?, bool)"/>
        public static async Task<UrlRecord> SaveSlugAsync<T>(this IUrlService service, 
            T entity,
            string seName,
            string displayName,
            bool ensureNotEmpty,
            int? languageId = null,
            bool force = false)
            where T : ISlugSupported
        {
            Guard.NotNull(entity);

            // Create lock handle to prevent another parallel request to recreate the slug.
            var lockHandle = (ILockHandle)null;
            var key = seName.NullEmpty() ?? displayName;

            if (ensureNotEmpty && string.IsNullOrEmpty(key))
            {
                // Use entity identifier as key if empty
                key = entity.GetEntityName().ToLower() + entity.Id.ToStringInvariant();
            }

            if (!string.IsNullOrEmpty(key))
            {
                var @lock = service.GetLock(key);

                // Try to acquire lock
                (await @lock.TryAcquireAsync(TimeSpan.FromSeconds(3))).Out(out lockHandle);
            }

            lockHandle ??= NullLockHandle.Instance;

            using (lockHandle)
            {
                // Validate (key is seName now)
                var slugResult = await service.ValidateSlugAsync(entity, key, displayName, ensureNotEmpty, languageId, force);

                // Must be saved immediately
                var entry = await service.ApplySlugAsync(slugResult, true);

                return entry;
            }
        }
    }
}
