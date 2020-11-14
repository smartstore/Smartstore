using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Smartstore.Core.Configuration;
using Smartstore.Core.Stores;
using Smartstore.Domain;
using Smartstore.Engine;

namespace Smartstore.Core.Localization
{
    // TODO: (core) Make localization extensions/helpers for: ICategoryNode

    public static partial class LocalizationExtensions
    {
        #region Entity

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

        #endregion

        #region Settings

        /// <summary>
        /// Get localized property of an <see cref="ISettings"/> implementation
        /// </summary>
        /// <param name="settings">The settings instance</param>
        /// <param name="keySelector">Key selector</param>
        /// <returns>Localized property</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LocalizedValue<string> GetLocalizedSetting<TSetting>(this TSetting settings,
            Expression<Func<TSetting, string>> keySelector,
            int? storeId = null,
            bool returnDefaultValue = true,
            bool ensureTwoPublishedLanguages = true,
            bool detectEmptyHtml = false)
            where TSetting : class, ISettings
        {
            return GetLocalizedSetting(settings, keySelector, null, storeId, returnDefaultValue, ensureTwoPublishedLanguages, detectEmptyHtml);
        }

        /// <summary>
        /// Get localized property of an <see cref="ISettings"/> implementation
        /// </summary>
        /// <param name="settings">The settings instance</param>
        /// <param name="keySelector">Key selector</param>
        /// <param name="requestLanguageIdOrObj">Language id, <see cref="Language"/> object instance or <c>null</c></param>
        /// <returns>Localized property</returns>
        public static LocalizedValue<string> GetLocalizedSetting<TSetting>(this TSetting settings,
            Expression<Func<TSetting, string>> keySelector,
            object requestLanguageIdOrObj, // Id or Language
            int? storeId = null,
            bool returnDefaultValue = true,
            bool ensureTwoPublishedLanguages = true,
            bool detectEmptyHtml = false)
            where TSetting : class, ISettings
        {
            var helper = EngineContext.Current.ResolveService<LocalizedEntityHelper>();
            var invoker = keySelector.CompileFast();

            if (storeId == null)
            {
                storeId = EngineContext.Current.ResolveService<IStoreContext>().CurrentStore.Id;
            }

            // Make fallback only when storeId is 0 and the paramter says so.
            var localizedValue = GetValue(storeId.Value, storeId == 0 && returnDefaultValue);

            if (storeId > 0 && string.IsNullOrEmpty(localizedValue.Value))
            {
                localizedValue = GetValue(0, returnDefaultValue);
            }

            return localizedValue;

            LocalizedValue<string> GetValue(int id /* storeId */, bool doFallback)
            {
                return helper.GetLocalizedValue(
                    settings,
                    id,
                    typeof(TSetting).Name,
                    invoker.Property.Name,
                    (Func<TSetting, string>)invoker,
                    requestLanguageIdOrObj,
                    doFallback,
                    ensureTwoPublishedLanguages,
                    detectEmptyHtml);
            }
        }

        #endregion

        #region Enums

        /// <summary>
        /// Get localized value of an enum.
        /// </summary>
        /// <typeparam name="T">Type of enum</typeparam>
        /// <param name="enumValue">Enum value</param>
        /// <param name="languageId">Language identifier</param>
        /// <param name="hint">Whether to load the hint.</param>
        /// <returns>Localized value</returns>
        public static string GetLocalizedEnum<T>(this T enumValue, int languageId = 0, bool hint = false)
            where T : struct
        {
            return EngineContext.Current.ResolveService<LocalizedEntityHelper>()
                .GetLocalizedEnum<T>(enumValue, languageId, hint);
        }

        #endregion

        #region Modules

        /// <summary>
        /// Get localized property value of a module descriptor.
        /// </summary>
        /// <param name="module">Module descriptor</param>
        /// <param name="propertyName">Name of the property</param>
        /// <param name="languageId">Language identifier</param>
        /// <param name="returnDefaultValue">A value indicating whether to return default value (if localized is not found)</param>
        /// <returns>Localized value</returns>
        public static string GetLocalizedModuleProperty<T>(this ModuleDescriptor module, string propertyName, int languageId = 0, bool doFallback = true)
        {
            return EngineContext.Current.ResolveService<LocalizedEntityHelper>()
                .GetLocalizedModuleProperty(module, propertyName, languageId, doFallback);
        }

        #endregion
    }
}
