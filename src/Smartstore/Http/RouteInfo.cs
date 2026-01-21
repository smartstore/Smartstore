#nullable enable

using Microsoft.AspNetCore.Routing;
using NSJ = Newtonsoft.Json;
using STJ = System.Text.Json.Serialization;
using System.Text.Json;
using Smartstore.Json.Polymorphy;
using System.ComponentModel;

namespace Smartstore.Http;

[NSJ.JsonConverter(typeof(RouteInfoConverter))]
[STJ.JsonConverter(typeof(RouteInfoJsonConverter))]
public class RouteInfo
{
    public RouteInfo(RouteInfo cloneFrom)
        : this(cloneFrom.Action, cloneFrom.Controller, new RouteValueDictionary(cloneFrom.RouteValues))
    {
    }

    public RouteInfo(string action, object routeValues)
        : this(action, null, routeValues)
    {
    }

    public RouteInfo(string action, string? controller, object? routeValues)
        : this(action, controller, new RouteValueDictionary(routeValues))
    {
    }

    public RouteInfo(string action, IDictionary<string, object?> routeValues)
        : this(action, null, routeValues)
    {
    }

    public RouteInfo(string action, string? controller, IDictionary<string, object?> routeValues)
        : this(action, controller, new RouteValueDictionary(routeValues))
    {
        Guard.NotNull(routeValues);
    }

    public RouteInfo(string action, RouteValueDictionary routeValues)
        : this(action, null, routeValues)
    {
    }

    [NSJ.JsonConstructor]
    public RouteInfo(string action, string? controller, RouteValueDictionary routeValues)
    {
        Guard.NotEmpty(action);
        Guard.NotNull(routeValues);

        Action = action;
        Controller = controller;
        RouteValues = routeValues;
    }

    public string Action { get; }
    public string? Controller { get; }

    [DefaultValue("[]")]
    public RouteValueDictionary RouteValues { get; }
}

#region Newtonsoft.Json Converter

internal class RouteInfoConverter : NSJ.JsonConverter<RouteInfo>
{
    public override bool CanWrite
        => false;

    public override RouteInfo? ReadJson(NSJ.JsonReader reader, Type objectType, RouteInfo? existingValue, bool hasExistingValue, NSJ.JsonSerializer serializer)
    {
        string? action = null;
        string? controller = null;
        RouteValueDictionary? routeValues = null;

        reader.Read();
        while (reader.TokenType == NSJ.JsonToken.PropertyName)
        {
            string? a = reader.Value?.ToString();
            if (string.Equals(a, "Action", StringComparison.OrdinalIgnoreCase))
            {
                reader.Read();
                action = serializer.Deserialize<string>(reader);
            }
            else if (string.Equals(a, "Controller", StringComparison.OrdinalIgnoreCase))
            {
                reader.Read();
                controller = serializer.Deserialize<string>(reader);
            }
            else if (string.Equals(a, "RouteValues", StringComparison.OrdinalIgnoreCase))
            {
                reader.Read();
                routeValues = serializer.Deserialize<RouteValueDictionary>(reader);
            }
            else
            {
                reader.Skip();
            }

            reader.Read();
        }

        var routeInfo = Activator.CreateInstance(objectType, [action, controller, routeValues]);

        return (RouteInfo?)routeInfo;
    }

    public override void WriteJson(NSJ.JsonWriter writer, RouteInfo? value, NSJ.JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

#endregion

#region System.Text.Json Converter

internal sealed class RouteInfoJsonConverter : STJ.JsonConverter<RouteInfo>
{
    public override bool HandleNull => true;

    public override RouteInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        string? action = null;
        string? controller = null;
        RouteValueDictionary? routeValues = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string? propertyName = reader.GetString();
                reader.Read();

                if (string.Equals(propertyName, "Action", StringComparison.OrdinalIgnoreCase))
                {
                    action = reader.GetString();
                }
                else if (string.Equals(propertyName, "Controller", StringComparison.OrdinalIgnoreCase))
                {
                    controller = reader.GetString();
                }
                else if (string.Equals(propertyName, "RouteValues", StringComparison.OrdinalIgnoreCase))
                {
                    routeValues = JsonSerializer.Deserialize<RouteValueDictionary>(ref reader, options);
                }
            }
        }

        return new RouteInfo(action!, controller, routeValues ?? []);
    }

    public override void Write(Utf8JsonWriter writer, RouteInfo value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("Action", value.Action);
        
        if (value.Controller != null)
        {
            writer.WriteString("Controller", value.Controller);
        }
        
        writer.WritePropertyName("RouteValues");
        //JsonSerializer.Serialize(writer, value.RouteValues, options);
        options.SerializePolymorphic(writer, value.RouteValues.ToDictionary());

        writer.WriteEndObject();
    }
}

#endregion
