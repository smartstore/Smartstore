using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Smartstore.Core.Configuration;
using Smartstore.Core.Stores;
using Smartstore.Engine;

namespace Smartstore.Core.Localization
{
    // TODO: (core) Make localization extensions/helpers for: Modules, Enums, ICategoryNode, GetLocalizedEnum

    public static partial class ISettingsLocalizationExtensions
    {
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
    }
}
