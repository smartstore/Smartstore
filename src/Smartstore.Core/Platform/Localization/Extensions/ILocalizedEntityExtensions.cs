using System;
using System.Linq.Expressions;
using Smartstore.Domain;
using Smartstore.Engine;

namespace Smartstore.Core.Localization
{
    public static partial class ILocalizedEntityExtensions
    {
        /// <summary>
        /// Get localized property of an entity
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="keySelector">Key selector</param>
        /// <param name="detectEmptyHtml">When <c>true</c>, additionally checks whether the localized value contains empty HTML only and falls back to the default value if so.</param>
        /// <returns>Localized property</returns>
        public static LocalizedValue<string> GetLocalized<T>(this T entity, Expression<Func<T, string>> keySelector, bool detectEmptyHtml = false)
            where T : BaseEntity, ILocalizedEntity
        {
            var invoker = keySelector.CompileFast();
            return EngineContext.Current.ResolveService<LocalizedEntityHelper>().GetLocalizedValue(
                entity,
                entity.Id,
                entity.GetEntityName(),
                invoker.Property.Name,
                (Func<T, string>)invoker,
                null,
                detectEmptyHtml: detectEmptyHtml);
        }

        /// <summary>
        /// Get localized property of an entity
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="keySelector">Key selector</param>
        /// <param name="languageId">Language identifier</param>
        /// <param name="returnDefaultValue">A value indicating whether to return default value (if localized is not found)</param>
        /// <param name="ensureTwoPublishedLanguages">A value indicating whether to ensure that we have at least two published languages; otherwise, load only default value</param>
        /// <param name="detectEmptyHtml">When <c>true</c>, additionally checks whether the localized value contains empty HTML only and falls back to the default value if so.</param>
        /// <returns>Localized property</returns>
        public static LocalizedValue<string> GetLocalized<T>(this T entity,
            Expression<Func<T, string>> keySelector,
            int languageId,
            bool returnDefaultValue = true,
            bool ensureTwoPublishedLanguages = true,
            bool detectEmptyHtml = false)
            where T : BaseEntity, ILocalizedEntity
        {
            var invoker = keySelector.CompileFast();
            return EngineContext.Current.ResolveService<LocalizedEntityHelper>().GetLocalizedValue<T, string>(
                entity,
                entity.Id,
                entity.GetEntityName(),
                invoker.Property.Name,
                invoker,
                languageId,
                returnDefaultValue,
                ensureTwoPublishedLanguages,
                detectEmptyHtml);
        }

        /// <summary>
        /// Get localized property of an entity
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="localeKey">Key selector</param>
        /// <param name="languageId">Language identifier</param>
        /// <param name="returnDefaultValue">A value indicating whether to return default value (if localized is not found)</param>
        /// <param name="ensureTwoPublishedLanguages">A value indicating whether to ensure that we have at least two published languages; otherwise, load only default value</param>
        /// <param name="detectEmptyHtml">When <c>true</c>, additionally checks whether the localized value contains empty HTML only and falls back to the default value if so.</param>
        /// <returns>Localized property</returns>
        public static LocalizedValue<TProp> GetLocalized<T, TProp>(this T entity,
            string localeKey,
            TProp fallback,
            Language language,
            bool returnDefaultValue = true,
            bool ensureTwoPublishedLanguages = true,
            bool detectEmptyHtml = false)
            where T : BaseEntity, ILocalizedEntity
        {
            return EngineContext.Current.ResolveService<LocalizedEntityHelper>().GetLocalizedValue<T, TProp>(
                entity,
                entity.Id,
                entity.GetEntityName(),
                localeKey,
                x => fallback,
                language,
                returnDefaultValue,
                ensureTwoPublishedLanguages,
                detectEmptyHtml);
        }

        /// <summary>
        /// Get localized property of an entity
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="keySelector">Key selector</param>
        /// <param name="language">Language</param>
        /// <param name="returnDefaultValue">A value indicating whether to return default value (if localized is not found)</param>
        /// <param name="ensureTwoPublishedLanguages">A value indicating whether to ensure that we have at least two published languages; otherwise, load only default value</param>
        /// <param name="detectEmptyHtml">When <c>true</c>, additionally checks whether the localized value contains empty HTML only and falls back to the default value if so.</param>
        /// <returns>Localized property</returns>
        public static LocalizedValue<string> GetLocalized<T>(this T entity,
            Expression<Func<T, string>> keySelector,
            Language language,
            bool returnDefaultValue = true,
            bool ensureTwoPublishedLanguages = true,
            bool detectEmptyHtml = false)
            where T : BaseEntity, ILocalizedEntity
        {
            var invoker = keySelector.CompileFast();
            return EngineContext.Current.ResolveService<LocalizedEntityHelper>().GetLocalizedValue<T, string>(
                entity,
                entity.Id,
                entity.GetEntityName(),
                invoker.Property.Name,
                invoker,
                language,
                returnDefaultValue,
                ensureTwoPublishedLanguages,
                detectEmptyHtml);
        }

        /// <summary>
        /// Get localized property of an entity
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <typeparam name="TProp">Property type</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="keySelector">Key selector</param>
        /// <param name="languageId">Language identifier</param>
        /// <param name="returnDefaultValue">A value indicating whether to return default value (if localized is not found)</param>
        /// <param name="ensureTwoPublishedLanguages">A value indicating whether to ensure that we have at least two published languages; otherwise, load only default value</param>
        /// <param name="detectEmptyHtml">When <c>true</c>, additionally checks whether the localized value contains empty HTML only and falls back to the default value if so.</param>
        /// <returns>Localized property</returns>
        public static LocalizedValue<TProp> GetLocalized<T, TProp>(this T entity,
            Expression<Func<T, TProp>> keySelector,
            int languageId,
            bool returnDefaultValue = true,
            bool ensureTwoPublishedLanguages = true,
            bool detectEmptyHtml = false)
            where T : BaseEntity, ILocalizedEntity
        {
            var invoker = keySelector.CompileFast();
            return EngineContext.Current.ResolveService<LocalizedEntityHelper>().GetLocalizedValue(
                entity,
                entity.Id,
                entity.GetEntityName(),
                invoker.Property.Name,
                (Func<T, TProp>)invoker,
                languageId,
                returnDefaultValue,
                ensureTwoPublishedLanguages,
                detectEmptyHtml);
        }

        /// <summary>
        /// Get localized property of an entity
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <typeparam name="TProp">Property type</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="keySelector">Key selector</param>
        /// <param name="language">Language</param>
        /// <param name="returnDefaultValue">A value indicating whether to return default value (if localized is not found)</param>
        /// <param name="ensureTwoPublishedLanguages">A value indicating whether to ensure that we have at least two published languages; otherwise, load only default value</param>
        /// <param name="detectEmptyHtml">When <c>true</c>, additionally checks whether the localized value contains empty HTML only and falls back to the default value if so.</param>
        /// <returns>Localized property</returns>
        public static LocalizedValue<TProp> GetLocalized<T, TProp>(this T entity,
            Expression<Func<T, TProp>> keySelector,
            Language language,
            bool returnDefaultValue = true,
            bool ensureTwoPublishedLanguages = true,
            bool detectEmptyHtml = false)
            where T : BaseEntity, ILocalizedEntity
        {
            var invoker = keySelector.CompileFast();
            return EngineContext.Current.ResolveService<LocalizedEntityHelper>().GetLocalizedValue(
                entity,
                entity.Id,
                entity.GetEntityName(),
                invoker.Property.Name,
                (Func<T, TProp>)invoker,
                language,
                returnDefaultValue,
                ensureTwoPublishedLanguages,
                detectEmptyHtml);
        }
    }
}
