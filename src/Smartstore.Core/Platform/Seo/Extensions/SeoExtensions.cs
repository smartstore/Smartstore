using System.Runtime.CompilerServices;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Seo
{
    public static partial class SeoExtensions
    {
        public static string BuildSlug<T>(this T entity, int? languageId = null)
            where T : IDisplayedEntity
        {
            Guard.NotNull(entity, nameof(entity));

            var name = entity.GetDisplayName();
            if (entity is ILocalizedEntity le)
            {
                name = le.GetLocalized(entity.GetDisplayNameMemberNames()[0], name, languageId);
            }

            return SlugUtility.Slugify(name);
        }

        /// <summary>
        ///  Gets the seo friendly active url slug for a slug supporting entity.
        /// </summary>
        /// <typeparam name="T">Type of slug supporting entity</typeparam>
        /// <param name="entity">Entity instance</param>
        /// <param name="languageId">Language identifier</param>
        /// <param name="returnDefaultValue">A value indicating whether to return default value (if language specified one is not found)</param>
        /// <param name="ensureTwoPublishedLanguages">A value indicating whether to ensure that we have at least two published languages; otherwise, load only default value</param>
        /// <returns>SEO slug</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetActiveSlug<T>(this T entity,
            int? languageId = null,
            bool returnDefaultValue = true,
            bool ensureTwoPublishedLanguages = true)
            where T : BaseEntity, ISlugSupported
        {
            Guard.NotNull(entity, nameof(entity));

            return EngineContext.Current.Scope.ResolveOptional<LocalizedEntityHelper>()?.GetActiveSlug(
                entity.GetEntityName(),
                entity.Id,
                languageId,
                returnDefaultValue,
                ensureTwoPublishedLanguages);
        }

        /// <summary>
        /// Gets the seo friendly active url slug for a category node
        /// </summary>
        /// <param name="node">Node instance</param>
        /// <returns>SEO slug</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetActiveSlug(this ICategoryNode node)
        {
            Guard.NotNull(node, nameof(node));

            return EngineContext.Current.Scope.ResolveOptional<LocalizedEntityHelper>()?.GetActiveSlug(nameof(Category), node.Id, null);
        }

        /// <summary>
        ///  Gets the seo friendly active url slug for a slug supporting entity.
        /// </summary>
        /// <typeparam name="T">Type of slug supporting entity</typeparam>
        /// <param name="entity">Entity instance</param>
        /// <param name="languageId">Language identifier</param>
        /// <param name="returnDefaultValue">A value indicating whether to return default value (if language specified one is not found)</param>
        /// <param name="ensureTwoPublishedLanguages">A value indicating whether to ensure that we have at least two published languages; otherwise, load only default value</param>
        /// <returns>SEO slug</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<string> GetActiveSlugAsync<T>(this T entity,
            int? languageId = null,
            bool returnDefaultValue = true,
            bool ensureTwoPublishedLanguages = true)
            where T : BaseEntity, ISlugSupported
        {
            Guard.NotNull(entity, nameof(entity));

            return EngineContext.Current.Scope.ResolveOptional<LocalizedEntityHelper>()?.GetActiveSlugAsync(
                entity.GetEntityName(),
                entity.Id,
                languageId,
                returnDefaultValue,
                ensureTwoPublishedLanguages);
        }

        /// <summary>
        /// Gets the seo friendly active url slug for a category node
        /// </summary>
        /// <param name="node">Node instance</param>
        /// <returns>SEO slug</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<string> GetActiveSlugAsync(this ICategoryNode node)
        {
            Guard.NotNull(node, nameof(node));

            return EngineContext.Current.Scope.ResolveOptional<LocalizedEntityHelper>()?.GetActiveSlugAsync(nameof(Category), node.Id, null);
        }

        /// <inheritdoc cref="ValidateSlug{T}(T, string, string, bool, int?)"/>
        /// <param name="seName">Search engine name to validate. If <c>null</c> or empty, the slug will be resolved from "<paramref name="entity"/>.GetDisplayName()".</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValidateSlugResult ValidateSlug<T>(this T entity, string seName, bool ensureNotEmpty, int? languageId = null)
            where T : ISlugSupported
        {
            Guard.NotNull(entity, nameof(entity));

            return EngineContext.Current.Scope.Resolve<IUrlService>().ValidateSlugAsync(entity, seName, entity.GetDisplayName(), ensureNotEmpty, languageId).Await();
        }

        /// <summary>
        /// Slugifies and checks uniqueness of a given search engine name. If not unique, a number will be appended to the result slug.
        /// </summary>
        /// <typeparam name="T">Type of slug supporting entity</typeparam>
        /// <param name="entity">Entity instance</param>
        /// <param name="seName">Search engine name to validate. If <c>null</c> or empty, the slug will be resolved from <paramref name="displayName"/>.</param>
        /// <param name="displayName">Display name used to resolve the slug if <paramref name="seName"/> is empty.</param>
        /// <param name="ensureNotEmpty">Ensure that slug is not empty</param>
        /// <returns>A system unique slug</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValidateSlugResult ValidateSlug<T>(this T entity, string seName, string displayName, bool ensureNotEmpty, int? languageId = null)
            where T : ISlugSupported
        {
            Guard.NotNull(entity, nameof(entity));

            return EngineContext.Current.Scope.Resolve<IUrlService>().ValidateSlugAsync(entity, seName, displayName, ensureNotEmpty, languageId).Await();
        }

        /// <inheritdoc cref="ValidateSlugAsync{T}(T, string, string, bool, int?)"/>
        /// <param name="seName">Search engine name to validate. If <c>null</c> or empty, the slug will be resolved from "<paramref name="entity"/>.GetDisplayName()".</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueTask<ValidateSlugResult> ValidateSlugAsync<T>(this T entity, string seName, bool ensureNotEmpty, int? languageId = null)
            where T : ISlugSupported
        {
            Guard.NotNull(entity, nameof(entity));

            return EngineContext.Current.Scope.Resolve<IUrlService>().ValidateSlugAsync(entity, seName, entity.GetDisplayName(), ensureNotEmpty, languageId);
        }

        /// <summary>
        /// Slugifies and checks uniqueness of a given search engine name. If not unique, a number will be appended to the result slug.
        /// </summary>
        /// <typeparam name="T">Type of slug supporting entity</typeparam>
        /// <param name="entity">Entity instance</param>
        /// <param name="seName">Search engine name to validate. If <c>null</c> or empty, the slug will be resolved from <paramref name="displayName"/>.</param>
        /// <param name="displayName">Display name used to resolve the slug if <paramref name="seName"/> is empty.</param>
        /// <param name="ensureNotEmpty">Ensure that slug is not empty</param>
        /// <returns>A system unique slug</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueTask<ValidateSlugResult> ValidateSlugAsync<T>(this T entity, string seName, string displayName, bool ensureNotEmpty, int? languageId = null)
            where T : ISlugSupported
        {
            Guard.NotNull(entity, nameof(entity));

            return EngineContext.Current.Scope.Resolve<IUrlService>().ValidateSlugAsync(entity, seName, displayName, ensureNotEmpty, languageId);
        }
    }
}