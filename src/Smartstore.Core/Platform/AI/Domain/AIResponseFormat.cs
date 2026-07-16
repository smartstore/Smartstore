#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

namespace Smartstore.Core.AI;

/// <summary>
/// Describes the JSON schema of a structured AI response.
/// </summary>
public class AIResponseSchema
{
    // INFO: (perf) JsonSerializerOptions initialization is expensive.
    private static readonly JsonSerializerOptions _defaultJsonOptions;
    private static readonly ConditionalWeakTable<JsonSerializerOptions, JsonSerializerOptions> _jsonOptionsCache = new();

    private string _schemaJson;

    static AIResponseSchema()
    {
        _defaultJsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.General)
        {
            RespectNullableAnnotations = true
        };

        _defaultJsonOptions.MakeReadOnly();
    }

#pragma warning disable CS8618
    [SetsRequiredMembers]
    private AIResponseSchema(string name, JsonNode node)
    {
        Name = name;
        _schemaJson = node.ToJsonString();
    }
#pragma warning restore CS8618

    /// <summary>
    /// A short, provider-safe identifier for the schema.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The JSON schema string that describes the expected response object.
    /// Must be a valid JSON object. Setting this property parses and validates the JSON immediately.
    /// </summary>
    public required string SchemaJson
    {
        get => _schemaJson.NullEmpty() ?? string.Empty;
        set
        {
            Guard.NotEmpty(value);
            if (!TryParseSchemaJson(value, out _))
            {
                throw new JsonException("SchemaJson must contain a valid JSON object representing a JSON schema.");
            }
            _schemaJson = value;
        }
    }

    /// <summary>
    /// An optional description included in the generated schema.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Indicates whether the schema should be strict (no additional properties, all properties required)
    /// and whether the provider should enforce strict schema adherence if supported. Defaults to TRUE.
    /// </summary>
    public bool Strict { get; set; } = true;

    /// <summary>
    /// Creates an AI response format by exporting the JSON schema from a CLR type.
    /// </summary>
    public static AIResponseSchema FromType<T>(
        string name,
        string? description = null,
        JsonSerializerOptions? jsonOptions = null,
        bool strict = true)
    {
        var options = GetJsonSerializerOptions(jsonOptions);

        var schema = JsonSchemaExporter.GetJsonSchemaAsNode(
            options,
            typeof(T),
            new JsonSchemaExporterOptions
            {
                TreatNullObliviousAsNonNullable = true
            });

        if (strict)
        {
            PrepareStrictSchema(schema);
        }

        return new AIResponseSchema(name, schema)
        {
            Description = description,
            Strict = strict
        };
    }

    private static bool TryParseSchemaJson(string schemaJson, out JsonNode? node)
    {
        node = null;
        if (JsonNode.Parse(schemaJson) is not JsonObject schemaObject)
        {
            return false;
        }

        node = schemaObject;
        return true;
    }

    private static JsonSerializerOptions GetJsonSerializerOptions(JsonSerializerOptions? options)
    {
        // INFO: Creating JsonSerializerOptions is expensive (it builds internal converter caches),
        // so we never do it more than once for the same source options. We cache the adjusted
        // copy keyed by the original instance; when the caller drops the original options, the
        // weak-keyed entry is collected automatically.
        if (options == null)
        {
            return _defaultJsonOptions;
        }

        if (options.RespectNullableAnnotations)
        {
            return options;
        }

        return _jsonOptionsCache.GetValue(options, static original =>
        {
            return new JsonSerializerOptions(original) { RespectNullableAnnotations = true };
        });
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
