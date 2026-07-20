#nullable enable

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml;
using Humanizer;
using Smartstore.Json;

namespace Smartstore.Utilities;

public static class Prettifier
{
    public static string HumanizeBytes(long bytes)
    {
        string format = "#,#";

        if (bytes >= Math.Pow(1024, 3)) // > GB
        {
            format = "#,#.##";
        }
        else if (bytes >= Math.Pow(1024, 2)) // MB
        {
            format = "#,#.#";
        }

        return bytes.Bytes().Humanize(format);
    }

    public static string HumanizeTimeSpan(TimeSpan timeSpan, CultureInfo? culture = null)
    {
        return timeSpan.Humanize(timeSpan.TotalMinutes < 1 ? 1 : 2, culture ?? CultureInfo.InvariantCulture);
    }

    public static string? PrettifyXml(string? xml)
    {
        if (xml.IsEmpty() || xml.IsWhiteSpace())
        {
            return xml;
        }

        // First read the xml ignoring whitespace
        using var xmlReader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings { IgnoreWhitespace = true, CheckCharacters = false });

        // Then write it out with indentation
        var sb = new StringBuilder(xml.Length * 2);
        using (XmlWriter writer = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, IndentChars = "\t", CheckCharacters = false }))
        {
            writer.WriteNode(xmlReader, true);
        }

        var result = sb.ToString();
        return result;
    }

    public static string? PrettifyJson(string? json)
    {
        if (json.IsEmpty())
        {
            return json;
        }

        try
        {
            using var doc = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
                MaxDepth = 32
            });

            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

            WriteJsonElement(writer, doc.RootElement);
            writer.Flush();

            return Encoding.UTF8.GetString(stream.ToArray());
        }
        catch
        {
            return json;
        }
    }

    private static void WriteJsonElement(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    WriteJsonElement(writer, property.Value);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteJsonElement(writer, item);
                }
                writer.WriteEndArray();
                break;
            default:
                writer.WriteRawValue(element.GetRawText());
                break;
        }
    }
}