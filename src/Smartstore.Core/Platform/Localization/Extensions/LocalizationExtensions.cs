using System.Runtime.CompilerServices;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Configuration;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Localization
{
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
            where T : class, ILocalizedEntity
        {
            var invoker = keySelector.GetPropertyInvoker();
            return EngineContext.Current.Scope.ResolveOptional<LocalizedEntityHelper>()?.GetLocalizedValue(
                entity,
                entity.Id,
                entity.GetEntityName(),
                invoker.Property.Name,
                (Func<T, string>)invoker,
                null,
                detectEmptyHtml: detectEmptyHtml) ?? new LocalizedValue<string>(invoker.Invoke(entity));
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
            where T : class, ILocalizedEntity
        {
            var invoker = keySelector.GetPropertyInvoker();
            return EngineContext.Current.Scope.ResolveOptional<LocalizedEntityHelper>()?.GetLocalizedValue<T, string>(
                entity,
                entity.Id,
                entity.GetEntityName(),
                invoker.Property.Name,
                invoker,
                languageId,
                returnDefaultValue,
                ensureTwoPublishedLanguages,
                detectEmptyHtml) ?? new LocalizedValue<string>(invoker.Invoke(entity));
        }

        /// <summary>
        /// Get localized property of an entity
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <typeparam name="TProp">Property type</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="localeKey">Key selector</param>
        /// <param name="requestLanguageIdOrObj">Language identifier or object <see cref="Language"/> entity instance.</param>
        /// <param name="returnDefaultValue">A value indicating whether to return default value (if localized is not found)</param>
        /// <param name="ensureTwoPublishedLanguages">A value indicating whether to ensure that we have at least two published languages; otherwise, load only default value</param>
        /// <param name="detectEmptyHtml">When <c>true</c>, additionally checks whether the localized value contains empty HTML only and falls back to the default value if so.</param>
        /// <returns>Localized property</returns>
        public static LocalizedValue<TProp> GetLocalized<T, TProp>(this T entity,
            string localeKey,
            TProp fallback,
            object requestLanguageIdOrObj, // Id or Language
            bool returnDefaultValue = true,
            bool ensureTwoPublishedLanguages = true,
            bool detectEmptyHtml = false)
            where T : class, ILocalizedEntity
        {
            return EngineContext.Current.Scope.ResolveOptional<LocalizedEntityHelper>()?.GetLocalizedValue<T, TProp>(
                entity,
                entity.Id,
                entity.GetEntityName(),
                localeKey,
                x => fallback,
                requestLanguageIdOrObj,
                returnDefaultValue,
                ensureTwoPublishedLanguages,
                detectEmptyHtml) ?? new LocalizedValue<TProp>(fallback);
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
            where T : class, ILocalizedEntity
        {
            var invoker = keySelector.GetPropertyInvoker();
            return EngineContext.Current.Scope.ResolveOptional<LocalizedEntityHelper>()?.GetLocalizedValue<T, string>(
                entity,
                entity.Id,
                entity.GetEntityName(),
                invoker.Property.Name,
                invoker,
                language,
                returnDefaultValue,
                ensureTwoPublishedLanguages,
                detectEmptyHtml) ?? new LocalizedValue<string>(invoker.Invoke(entity));
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
            where T : class, ILocalizedEntity
        {
            var invoker = keySelector.GetPropertyInvoker();
            return EngineContext.Current.Scope.ResolveOptional<LocalizedEntityHelper>()?.GetLocalizedValue(
                entity,
                entity.Id,
                entity.GetEntityName(),
                invoker.Property.Name,
                (Func<T, TProp>)invoker,
                languageId,
                returnDefaultValue,
                ensureTwoPublishedLanguages,
                detectEmptyHtml) ?? new LocalizedValue<TProp>(invoker.Invoke(entity));
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
            where T : class, ILocalizedEntity
        {
            var invoker = keySelector.GetPropertyInvoker();
            return EngineContext.Current.Scope.ResolveOptional<LocalizedEntityHelper>()?.GetLocalizedValue(
                entity,
                entity.Id,
                entity.GetEntityName(),
                invoker.Property.Name,
                (Func<T, TProp>)invoker,
                language,
                returnDefaultValue,
                ensureTwoPublishedLanguages,
                detectEmptyHtml) ?? new LocalizedValue<TProp>(invoker.Invoke(entity));
        }

        #endregion

        #region ICategoryNode

        /// <summary>
        /// Get localized property of an <see cref="ICategoryNode"/> instance
        /// </summary>
        /// <param name="node">Node</param>
        /// <param name="keySelector">Key selector</param>
        /// <returns>Localized property</returns>
        public static LocalizedValue<string> GetLocalized(this ICategoryNode node, Expression<Func<ICategoryNode, string>> keySelector)
        {
            var invoker = keySelector.GetPropertyInvoker();
            return EngineContext.Current.Scope.ResolveOptional<LocalizedEntityHelper>()?.GetLocalizedValue(
                node,
                node.Id,
                nameof(Category),
                invoker.Property.Name,
                (Func<ICategoryNode, string>)invoker,
                null) ?? new LocalizedValue<string>(invoker.Invoke(node));
        }

        /// <summary>
        /// Get localized property of an <see cref="ICategoryNode"/> instance
        /// </summary>
        /// <param name="node">Node</param>
        /// <param name="keySelector">Key selector</param>
        /// /// <param name="languageId">Language identifier</param>
        /// <returns>Localized property</returns>
        public static LocalizedValue<string> GetLocalized(this ICategoryNode node, Expression<Func<ICategoryNode, string>> keySelector, int languageId)
        {
            var invoker = keySelector.GetPropertyInvoker();
            return EngineContext.Current.Scope.ResolveOptional<LocalizedEntityHelper>()?.GetLocalizedValue(
                node,
                node.Id,
                nameof(Category),
                invoker.Property.Name,
                (Func<ICategoryNode, string>)invoker,
                languageId) ?? new LocalizedValue<string>(invoker.Invoke(node));
        }

        /// <summary>
        /// Get localized property of an <see cref="ICategoryNode"/> instance
        /// </summary>
        /// <param name="node">Node</param>
        /// <param name="keySelector">Key selector</param>
        /// /// <param name="language">Language</param>
        /// <returns>Localized property</returns>
        public static LocalizedValue<string> GetLocalized(this ICategoryNode node, Expression<Func<ICategoryNode, string>> keySelector, Language language)
        {
            var invoker = keySelector.GetPropertyInvoker();
            return EngineContext.Current.Scope.Resolve<LocalizedEntityHelper>()?.GetLocalizedValue(
                node,
                node.Id,
                nameof(Category),
                invoker.Property.Name,
                (Func<ICategoryNode, string>)invoker,
                language) ?? new LocalizedValue<string>(invoker.Invoke(node));
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
            var helper = EngineContext.Current.Scope.ResolveOptional<LocalizedEntityHelper>();
            var invoker = keySelector.GetPropertyInvoker();

            if (helper == null)
            {
                return new LocalizedValue<string>(invoker.Invoke(settings));
            }

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
        /// Gets the localized value of an enum.
        /// </summary>
        /// <typeparam name="T">Enum type.</typeparam>
        /// <param name="enumValue">Enum value.</param>
        /// <param name="languageId">Language identifier.</param>
        /// <param name="hint">A value indicating whether to load the hint.</param>
        /// <returns>Localized value of an enum.</returns>
        public static string GetLocalizedEnum<T>(this T enumValue, int languageId = 0, bool hint = false)
            where T : struct
        {
            return EngineContext.Current.ResolveService<ILocalizationService>()
                .GetLocalizedEnum(enumValue, languageId, hint) ?? enumValue.ToString();
        }

        /// <summary>
        /// Gets the localized value of an enum.
        /// </summary>
        /// <typeparam name="T">Enum type.</typeparam>
        /// <param name="enumValue">Enum value.</param>
        /// <param name="languageId">Language identifier.</param>
        /// <param name="hint">A value indicating whether to load the hint.</param>
        /// <returns>Localized value of an enum.</returns>
        public static async Task<string> GetLocalizedEnumAsync<T>(this T enumValue, int languageId = 0, bool hint = false)
            where T : struct
        {
            return await EngineContext.Current.ResolveService<ILocalizationService>()
                .GetLocalizedEnumAsync(enumValue, languageId, hint) ?? enumValue.ToString();
        }

        #endregion

        #region Modules

        /// <summary>
        /// Get localized property value of a module descriptor.
        /// </summary>
        /// <param name="module">Module descriptor</param>
        /// <param name="propertyName">Name of the property</param>
        /// <param name="languageId">Language identifier</param>
        /// <param name="doFallback">A value indicating whether to return default value (if localized is not found)</param>
        /// <returns>Localized value</returns>
        public static string GetLocalizedModuleProperty<T>(this IModuleDescriptor module, string propertyName, int languageId = 0, bool doFallback = true)
        {
            return EngineContext.Current.Scope.ResolveOptional<LocalizedEntityHelper>()?
                .GetLocalizedModuleProperty(module, propertyName, languageId, doFallback);
        }

        #endregion
    }
}
