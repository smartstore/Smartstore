using System.Globalization;
using System.Text.Json;
using Smartstore.Json;

namespace Smartstore.ComponentModel.TypeConverters;

internal class JsonTypeConverter : DefaultTypeConverter
{
    private readonly Type _type;

    public JsonTypeConverter(Type type)
        : base(type)
    {
        _type = type;
    }

    public override bool CanConvertFrom(Type type)
        => type == typeof(string);

    public override bool CanConvertTo(Type type)
        => type == typeof(string);

    public override object ConvertFrom(CultureInfo culture, object value)
    {
        if (value != null && value is not string)
        {
            return base.ConvertFrom(culture, value);
        }

        var json = value as string;
        var result = json.HasValue()
            ? JsonSerializer.Deserialize(json, _type, SmartJsonOptions.Default)
            : null;

        return result == null && _type.IsArray
            ? Array.CreateInstance(_type.GetElementType(), 0)
            : result;
    }

    public override object ConvertTo(CultureInfo culture, string format, object value, Type to)
    {
        if (to != typeof(string))
        {
            return base.ConvertTo(culture, format, value, to);
        }

        return value == null || value is Array { Length: 0 }
            ? string.Empty
            : JsonSerializer.Serialize(value, _type, SmartJsonOptions.Default);
    }
}
