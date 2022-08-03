using Smartstore.Core.Configuration;

namespace Smartstore.Core.Data.Migrations
{
    public static class SeedEntityExtensions
    {
        public static SeedEntityAlterer<T, TKey> WithKey<T, TKey>(this IList<T> list, Func<T, TKey> predicate) where T : BaseEntity
            => new(list, predicate);

        public static SeedSettingsAlterer Alter<TSettings>(this IList<ISettings> list, Action<TSettings> action) where TSettings : class, ISettings, new()
        {
            var alterer = new SeedSettingsAlterer(list);
            return alterer.Alter(action);
        }
    }
}
