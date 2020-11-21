using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Smartstore.ComponentModel.JsonConverters;

namespace Smartstore.Caching.JsonConverters
{
    public sealed class CacheEntryJsonConverter : ObjectContainerJsonConverter<CacheEntry>
    {
        protected override void ReadProperty(ref Utf8JsonReader reader, string propertyName, CacheEntry result, JsonSerializerOptions options)
        {
            switch (propertyName)
            {
                case nameof(CacheEntry.Key):
                    result.Key = reader.GetString();
                    break;
                case nameof(CacheEntry.Tag):
                    result.Tag = reader.GetString();
                    break;
                case nameof(CacheEntry.CachedOn):
                    result.CachedOn = reader.GetDateTimeOffset();
                    break;
                case nameof(CacheEntry.LastAccessedOn):
                    result.LastAccessedOn = reader.GetDateTimeOffset();
                    break;
                case nameof(CacheEntry.Duration):
                    if (TimeSpan.TryParse(reader.GetString(), out var duration))
                    {
                        result.Duration = duration;
                    }
                    break;
                case nameof(CacheEntry.Priority):
                    result.Priority = (CacheEntryPriority)reader.GetInt32();
                    break;
                case nameof(CacheEntry.Dependencies):
                    result.Dependencies = JsonSerializer.Deserialize<string[]>(ref reader, options);
                    break;
            }
        }

        protected override void WriteCore(Utf8JsonWriter writer, CacheEntry value, JsonSerializerOptions options)
        {
            writer.WriteString(nameof(CacheEntry.Key), value.Key);
            writer.WriteString(nameof(CacheEntry.Tag), value.Tag);
            writer.WriteString(nameof(CacheEntry.CachedOn), value.CachedOn);
            writer.WriteString(nameof(CacheEntry.LastAccessedOn), value.LastAccessedOn);
            writer.WriteString(nameof(CacheEntry.Duration), value.Duration?.ToString());
            writer.WriteNumber(nameof(CacheEntry.Priority), (int)value.Priority);

            writer.WritePropertyName(nameof(CacheEntry.Dependencies));
            var converter = (JsonConverter<string[]>)options.GetConverter(typeof(string[]));
            if (converter != null)
            {
                converter.Write(writer, value.Dependencies, options);
            }
            else
            {
                JsonSerializer.Serialize(writer, value.Dependencies, typeof(string[]), options);
            }
        }
    }
}