namespace Smartstore.Core.Seo
{
    public static partial class IUrlServiceExtensions
    {
        /// <summary>
        /// Applies a slug without sanitization or uniqueness check. This method
        /// throws if the given slug already exists in the database. The recommended
        /// way to apply a slug is to call <see cref="IUrlService.ValidateSlugAsync{T}(T, string, string, bool, int?)"/>
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
            Guard.NotNull(entity, nameof(entity));

            var input = new ValidateSlugResult
            {
                Source = entity,
                Slug = slug,
                LanguageId = languageId
            };

            return service.ApplySlugAsync(input, save);
        }

        /// <inheritdoc cref="IUrlService.ValidateSlugAsync{T}(T, string, string, bool, int?)"/>
        /// <param name="seName">Search engine name to validate. If <c>null</c> or empty, the slug will be resolved from "<paramref name="entity"/>.GetDisplayName()".</param>
        public static ValueTask<ValidateSlugResult> ValidateSlugAsync<T>(this IUrlService service, T entity, string seName, bool ensureNotEmpty, int? languageId = null)
            where T : ISlugSupported
        {
            return service.ValidateSlugAsync(entity, seName, entity.GetDisplayName(), ensureNotEmpty, languageId);
        }
    }
}
