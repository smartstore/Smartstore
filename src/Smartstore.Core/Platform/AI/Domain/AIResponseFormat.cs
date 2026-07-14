#nullable enable

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

namespace Smartstore.Core.AI;

/// <summary>
/// Describes the expected structure of an AI text response.
/// </summary>
public class AIResponseFormat
{
    /// <summary>
    /// Gets or sets a short, provider-safe name for the schema.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets a JSON schema that describes the expected response object.
    /// </summary>
    public required string JsonSchema { get; set; }

    /// <summary>
    /// Gets or sets an optional description of the schema.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether the provider should enforce strict schema adherence if supported.
    /// </summary>
    public bool Strict { get; set; } = true;

    /// <summary>
    /// Creates an AI response format by exporting the JSON schema from a CLR type.
    /// </summary>
    public static AIResponseFormat FromType<T>(
        string name,
        string? description = null,
        JsonSerializerOptions? jsonOptions = null,
        bool strict = true)
    {
        var options = jsonOptions != null
            ? new JsonSerializerOptions(jsonOptions)
            : new JsonSerializerOptions(JsonSerializerDefaults.General);

        options.RespectNullableAnnotations = true;

        var schema = JsonSchemaExporter.GetJsonSchemaAsNode(
            options,
            typeof(T),
            new JsonSchemaExporterOptions
            {
                TreatNullObliviousAsNonNullable = true
            });

        PrepareStrictSchema(schema);

        return new AIResponseFormat
        {
            Name = name,
            Description = description,
            JsonSchema = schema.ToJsonString(),
            Strict = strict
        };
    }

    private static void PrepareStrictSchema(JsonNode? schema)
    {
        if (schema is JsonObject obj)
        {
            if (IsObjectSchema(obj))
            {
                obj["additionalProperties"] = false;
                RequireAllProperties(obj);
            }

            foreach (var property in obj.ToArray())
            {
                PrepareStrictSchema(property.Value);
            }
        }
        else if (schema is JsonArray array)
        {
            foreach (var item in array)
            {
                PrepareStrictSchema(item);
            }
        }
    }

    private static bool IsObjectSchema(JsonObject schema)
    {
        if (schema.ContainsKey("properties"))
        {
            return true;
        }

        return schema["type"] is JsonValue value
            && value.TryGetValue<string>(out var type)
            && type == "object";
    }

    private static void RequireAllProperties(JsonObject schema)
    {
        if (schema["properties"] is not JsonObject properties)
        {
            return;
        }

        var required = new JsonArray();
        foreach (var property in properties)
        {
            required.Add(property.Key);
        }

        schema["required"] = required;
    }
}
