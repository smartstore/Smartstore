using Smartstore.Core.Configuration;

namespace Smartstore.Core.Data.Migrations
{
    public class SeedSettingsAlterer
    {
        private readonly Dictionary<Type, ISettings> _settingsMap;

        public SeedSettingsAlterer(IList<ISettings> settings)
        {
            // fetch all types from list and build a key/value map for faster access.
            _settingsMap = new Dictionary<Type, ISettings>(settings.Count);

            foreach (var setting in settings)
            {
                _settingsMap.Add(setting.GetType(), setting);
            }
        }

        public SeedSettingsAlterer Alter<TSettings>(Action<TSettings> action) where TSettings : class, ISettings, new()
        {
            if (_settingsMap.TryGetValue(typeof(TSettings), out var setting))
            {
                action(setting as TSettings);
            }

            return this;
        }
    }

    public class SeedEntityAlterer<T, TKey> where T : BaseEntity
    {
        private readonly Dictionary<TKey, T> _entityMap;
        private readonly IList<T> _entities;

        public SeedEntityAlterer(IList<T> list, Func<T, TKey> keyPredicate)
        {
            _entities = list;

            // fetch all key values from list and build a key/value map for faster access.
            _entityMap = new Dictionary<TKey, T>(list.Count);
            var fn = keyPredicate;

            foreach (var entity in list)
            {
                var key = fn.Invoke(entity);
                _entityMap.Add(key, entity);
            }

        }

        public SeedEntityAlterer<T, TKey> Alter(TKey key, Action<T> action)
        {
            if (_entityMap.TryGetValue(key, out var entity))
            {
                action(entity);
            }

            return this;
        }

        public SeedEntityAlterer<T, TKey> Remove(TKey key)
        {
            if (_entityMap.TryGetValue(key, out var entity))
            {
                _entityMap.Remove(key);
                _entities.Remove(entity);
            }

            return this;
        }
    }
}