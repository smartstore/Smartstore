using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Smartstore.IconKit;

// Bind the compact expression directly, including an empty selection, without surrogate model properties.
internal sealed class IconExpressionTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        => value is string text
            ? (string.IsNullOrWhiteSpace(text) ? default(IconExpression) : IconExpression.Parse(text))
            : base.ConvertFrom(context, culture, value);
}

internal sealed class IconExpressionJsonConverter : JsonConverter<IconExpression>
{
    public override IconExpression Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString();
        return string.IsNullOrWhiteSpace(text) ? default : IconExpression.Parse(text);
    }

    public override void Write(Utf8JsonWriter writer, IconExpression value, JsonSerializerOptions options)
        => writer.WriteStringValue(value == default ? string.Empty : value.ToString());
}
