using System.Runtime.CompilerServices;
using Smartstore.ComponentModel;
using Smartstore.Core.Configuration;

namespace Smartstore
{
    public static class ISettingServiceExtensions
    {
        /// <summary>
        /// Checks whether a setting for the given store exists.
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <typeparam name="TPropType">Property type</typeparam>
        /// <param name="settings">Settings</param>
        /// <param name="keySelector">Key selector</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns><c>true</c> if setting exists in database.</returns>
        public static async Task<bool> SettingExistsAsync<T, TPropType>(this ISettingService service,
            T settings,
            Expression<Func<T, TPropType>> keySelector,
            int storeId = 0)
            where T : ISettings, new()
        {
            Guard.NotNull(keySelector, nameof(keySelector));

            var propInfo = keySelector.ExtractPropertyInfo();
            var key = string.Concat(typeof(T).Name, ".", propInfo.Name);

            return await service.SettingExistsAsync(key, storeId);
        }

        /// <summary>
        /// Applies setting value. The caller is responsible for database commit.
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <typeparam name="TPropType">Property type</typeparam>
        /// <param name="settings">Settings</param>
        /// <param name="keySelector">Key selector</param>
        /// <param name="storeId">Store ID</param>
        public static async Task<ApplySettingResult> ApplySettingAsync<T, TPropType>(this ISettingService service,
            T settings,
            Expression<Func<T, TPropType>> keySelector,
            int storeId = 0)
            where T : ISettings, new()
        {
            var propInfo = keySelector.ExtractPropertyInfo();
            var key = string.Concat(typeof(T).Name, ".", propInfo.Name);

            // Duck typing is not supported in C#. That's why we're using dynamic type.
            dynamic currentValue = propInfo.GetValue(settings);

            return await service.ApplySettingAsync(key, currentValue, storeId);
        }

        /// <summary>
        /// Remove all settings. The caller is responsible for database commit.
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<int> RemoveSettingsAsync<T>(this ISettingService service)
            where T : ISettings, new()
        {
            return await service.RemoveSettingsAsync(typeof(T).Name);
        }

        /// <summary>
        /// Removes a setting. The caller is responsible for database commit.
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <typeparam name="TPropType">Property type</typeparam>
        /// <param name="settings">Settings</param>
        /// <param name="keySelector">Key selector</param>
        /// <param name="storeId">Store ID</param>
        /// <returns><c>true</c> when the setting existed and has been deleted</returns>
        public static async Task<bool> RemoveSettingAsync<T, TPropType>(this ISettingService service,
            T settings,
            Expression<Func<T, TPropType>> keySelector,
            int storeId = 0)
            where T : ISettings, new()
        {
            var propInfo = keySelector.ExtractPropertyInfo();
            var key = string.Concat(typeof(T).Name, ".", propInfo.Name);

            return await service.RemoveSettingAsync(key, storeId);
        }

        /// <summary>
        /// Applies or removes a setting property. The caller is responsible for database commit.
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <typeparam name="TPropType">Property type</typeparam>
        /// <param name="settings">Settings</param>
        /// <param name="keySelector">Key selector</param>
        /// <param name="storeId">Store ID</param>
        /// <returns><c>true</c> when the setting property has been modified.</returns>
        public static async Task<ApplySettingResult> UpdateSettingAsync<T, TPropType>(this ISettingService service,
            T settings,
            Expression<Func<T, TPropType>> keySelector,
            bool overrideForStore,
            int storeId = 0) where T : ISettings, new()
        {
            if (overrideForStore || storeId == 0)
            {
                return await service.ApplySettingAsync(settings, keySelector, storeId);
            }
            else if (storeId > 0 && await service.RemoveSettingAsync(settings, keySelector, storeId))
            {
                return ApplySettingResult.Deleted;
            }

            return ApplySettingResult.Unchanged;
        }
    }
}