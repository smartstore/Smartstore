using Smartstore.Core.Configuration;
using Smartstore.Utilities;

namespace Smartstore.Core.Data.Migrations
{
    internal class SettingEntry
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public string DefaultValue { get; set; }
        public bool KeyIsGroup { get; set; }
        public SettingEntryOperation Operation { get; set; }
    }

    internal enum SettingEntryOperation
    {
        Add,
        Update,
        Delete
    }

    public class SettingsBuilder : IHideObjectMembers
    {
        private readonly List<SettingEntry> _entries = new();

        /// <summary>
        /// Deletes one or many setting records from the database
        /// </summary>
        /// <param name="keys">The key(s) of the settings to delete</param>
        public void Delete(params string[] keys)
        {
            keys.Each(x => _entries.Add(new SettingEntry 
            { 
                Key = x.TrimSafe(), 
                Operation = SettingEntryOperation.Delete  
            }));
        }

        /// <summary>
        /// Deletes all settings records prefixed with the specified group name from the database
        /// </summary>
        /// <param name="group">The group/prefix (actually the settings class name)</param>
        public void DeleteGroup(string group)
        {
            Guard.NotEmpty(group);

            _entries.Add(new SettingEntry 
            { 
                Key = group.Trim(), 
                KeyIsGroup = true, 
                Operation = SettingEntryOperation.Delete 
            });
        }

        /// <summary>
        /// Adds a setting if it doesn't exist yet.
        /// </summary>
        public void Add<TValue>(string key, TValue value)
        {
            Guard.NotEmpty(key);

            _entries.Add(new SettingEntry 
            { 
                Key = key.Trim(), 
                Value = value.Convert(string.Empty), 
                Operation = SettingEntryOperation.Add 
            });
        }

        /// <summary>
        /// Updates an existing setting.
        /// </summary>
        public void Update<TSetting>(Expression<Func<TSetting, object>> propertyAccessor, object value)
            where TSetting : ISettings
        {
            Update(propertyAccessor, value, null);
        }

        /// <summary>
        /// Updates an existing setting, but only if <paramref name="defaultValue"/> is not equal to the setting's value.
        /// </summary>
        public void Update<TSetting>(Expression<Func<TSetting, object>> propertyAccessor, object value, object defaultValue)
            where TSetting : ISettings
        {
            Guard.NotNull(propertyAccessor);

            var key = TypeHelper.NameOf<TSetting>(propertyAccessor, true);
            UpdateInternal(key, value.Convert(string.Empty), defaultValue.Convert<string>());
        }

        /// <summary>
        /// Updates an existing setting.
        /// </summary>
        public void Update<TValue>(string key, TValue value)
        {
            UpdateInternal(key, value.Convert(string.Empty), null);
        }

        /// <summary>
        /// Updates an existing setting, but only if <paramref name="defaultValue"/> is not equal to the setting's value.
        /// </summary>
        public void Update<TValue>(string key, TValue value, TValue defaultValue)
        {
            UpdateInternal(key, value.Convert(string.Empty), defaultValue.Convert<string>());
        }

        private void UpdateInternal(string key, string value, string defaultValue)
        {
            Guard.NotEmpty(key);

            _entries.Add(new SettingEntry
            {
                Key = key.Trim(),
                Value = value,
                DefaultValue = defaultValue,
                Operation = SettingEntryOperation.Update
            });
        }

        internal void Reset()
        {
            _entries.Clear();
        }

        internal IEnumerable<SettingEntry> Build()
        {
            return _entries.Where(x => x.Key.HasValue());
        }
    }
}
