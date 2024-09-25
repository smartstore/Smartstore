using System.Linq.Dynamic.Core;
using System.Reflection;
using Autofac;
using Smartstore.ComponentModel;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Localization
{
    internal static class LocalizedSettingsLoader
    {
        private static Dictionary<string, LocalizedSettingEntry> _entries = null;
        private static object _lock = new();

        private static void Initialize(ITypeScanner typeScanner, Dictionary<string, LocalizedSettingEntry> entries)
        {
            foreach (var type in typeScanner.FindTypes<ISettings>())
            {
                if (typeof(ILocalizedEntity).IsAssignableFrom(type))
                {
                    continue;
                }

                var candidateProperties = FastProperty.GetCandidateProperties(type)
                    .Where(x => x.HasAttribute<LocalizedPropertyAttribute>(true));

                foreach (var prop in candidateProperties)
                {
                    var key = $"{type.Name}.{prop.Name}";
                    entries.Add(key, new LocalizedSettingEntry
                    {
                        FullKey = key,
                        EntityType = type,
                        Property = prop
                    });
                }
            }
        }

        internal static async Task<IList<dynamic>> LoadLocalizedSettings(ILifetimeScope scope, SmartDbContext db)
        {
            _entries = LazyInitializer.EnsureInitialized(ref _entries, ref _lock, () => 
            {
                _entries = new(StringComparer.OrdinalIgnoreCase);
                Initialize(scope.Resolve<ITypeScanner>(), _entries);
                return _entries;
            });

            var result = new List<dynamic>();

            if (_entries.Count == 0)
            {
                return result;
            }

            // Load only the Setting entities that represent localizable properties.
            var keys = _entries.Keys.ToArray();
            var localizedSettings = await db.Settings
                .AsNoTracking()
                .AsNoCaching()
                .Where(x => keys.Contains(x.Name) && !string.IsNullOrEmpty(x.Value))
                .ToListAsync();

            foreach (var setting in localizedSettings)
            {
                // Get entry for this setting
                if (_entries.TryGetValue(setting.Name, out var entry))
                {
                    // Create dynamic class in the shape: new { Id = 0, KeyGroup = "BlogSettings", MetaTitle = "Title" }
                    var dynamicProps = new List<DynamicProperty>
                    {
                        new("Id", typeof(int)),
                        new("KeyGroup", typeof(string)),
                        new(entry.Property.Name, entry.Property.PropertyType)
                    };
                    var dynamicClassType = DynamicClassFactory.CreateType(dynamicProps, true);

                    var obj = (DynamicClass)Activator.CreateInstance(dynamicClassType, new object[] 
                    {
                        // INFO: We "misuse" StoreId as Id for setting types
                        setting.StoreId,
                        entry.EntityType.Name,
                        setting.Value
                    });

                    result.Add(obj);
                }
            }

            return result;
        }

        readonly struct LocalizedSettingEntry
        {
            public string FullKey { get; init; }
            public Type EntityType { get; init; }
            public PropertyInfo Property { get; init; }
        }
    }
}
