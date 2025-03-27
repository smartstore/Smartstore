#nullable enable

using System.Runtime.CompilerServices;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Seo;

namespace Smartstore.Web.Rendering
{
    public static class UrlHelperRenderingExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Media(this IUrlHelper url, IImageModel model)
        {
            Guard.NotNull(url);
            Guard.NotNull(model);

            var mediaService = url.ActionContext.HttpContext.RequestServices.GetRequiredService<IMediaService>();

            return mediaService.GetUrl(model.File, model.ThumbSize ?? 0, model.Host, !model.NoFallback);
        }

        /// <summary>
        /// Generates a URL to patch an entity.
        /// </summary>
        /// <param name="entityName">The name of the entity.</param>
        /// <param name="propertyName">The name of the property to patch.</param>
        /// <param name="entityId">The ID of the entity.</param>
        /// <returns>The generated URL or <c>null</c> if <paramref name="entityId"/> is 0.</returns>
        public static string? PatchEntity(this IUrlHelper url, string entityName, string propertyName, int entityId)
        {
            Guard.NotNull(url);
            Guard.NotEmpty(entityName);
            Guard.NotEmpty(propertyName);

            if (entityId < 1)
            {
                return null;
            }

            return url.Action("Patch", "Entity", new { area = string.Empty, entityName, propertyName, entityId });
        }

        /// <summary>
        /// Generates a URL to patch an entity of a specified type.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="propertyName">The name of the property to patch.</param>
        /// <param name="entityId">The ID of the entity.</param>
        /// <returns>The generated URL or <c>null</c> if <paramref name="entityId"/> is 0.</returns>
        public static string? PatchEntity<TEntity>(this IUrlHelper url, string propertyName, int entityId) where TEntity : BaseEntity
        {
            Guard.NotNull(url);
            Guard.NotEmpty(propertyName);

            if (entityId < 1)
            {
                return null;
            }

            return url.Action("Patch", "Entity", new { area = string.Empty, entityName = typeof(TEntity).FullName, propertyName, entityId });
        }

        /// <summary>
        /// Generates a URL to patch a localized entity.
        /// </summary>
        /// <param name="entityName">The name of the entity.</param>
        /// <param name="propertyName">The name of the property to patch.</param>
        /// <param name="entityId">The ID of the entity.</param>
        /// <param name="languageId">The ID of the language.</param>
        /// <returns>The generated URL or <c>null</c> if <paramref name="entityId"/> is 0.</returns>
        public static string? PatchLocalizedEntity(this IUrlHelper url, string entityName, string propertyName, int entityId, int languageId)
        {
            Guard.NotNull(url);
            Guard.NotEmpty(entityName);
            Guard.NotEmpty(propertyName);

            if (entityId < 1)
            {
                return null;
            }

            return url.Action("PatchLocalized", "Entity", new { area = string.Empty, entityName, propertyName, entityId, languageId });
        }

        /// <summary>
        /// Generates a URL to patch a localized entity.
        /// </summary>
        /// <param name="propertyName">The name of the property to patch.</param>
        /// <param name="entityId">The ID of the entity.</param>
        /// <param name="languageId">The ID of the language.</param>
        /// <returns>The generated URL or <c>null</c> if <paramref name="entityId"/> is 0.</returns>
        public static string? PatchLocalizedEntity<TEntity>(this IUrlHelper url, string propertyName, int entityId, int languageId) where TEntity : BaseEntity, new()
        {
            Guard.NotNull(url);
            Guard.NotEmpty(propertyName);

            if (entityId < 1)
            {
                return null;
            }

            return url.Action("PatchLocalized", "Entity", new { area = string.Empty, entityName = NamedEntity.GetEntityName<TEntity>(), propertyName, entityId, languageId });
        }
    }
}
