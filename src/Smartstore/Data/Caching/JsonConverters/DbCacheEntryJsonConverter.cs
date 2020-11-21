using System.Text.Json;
using Smartstore.ComponentModel.JsonConverters;

namespace Smartstore.Data.Caching.JsonConverters
{
    public sealed class DbCacheEntryJsonConverter : ObjectContainerJsonConverter<DbCacheEntry>
    {
        protected override void ReadProperty(ref Utf8JsonReader reader, string propertyName, DbCacheEntry result, JsonSerializerOptions options)
        {
            if (propertyName == nameof(DbCacheEntry.Key))
            {
                result.Key = JsonSerializer.Deserialize<DbCacheKey>(ref reader, options);
            }
        }

        protected override void WriteCore(Utf8JsonWriter writer, DbCacheEntry value, JsonSerializerOptions options)
        {
            writer.WritePropertyName(nameof(DbCacheEntry.Key));
            JsonSerializer.Serialize(writer, value.Key, typeof(DbCacheKey), options);
        }
    }
}