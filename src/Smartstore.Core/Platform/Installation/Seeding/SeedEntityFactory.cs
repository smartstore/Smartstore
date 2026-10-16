using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Smartstore.Core.Installation.Seeding;

/// <summary>
/// Context passed to field handlers of a <see cref="SeedEntityDescriptor{T}"/>.
/// </summary>
public sealed class SeedBindingContext
{
    internal SeedBindingContext(SeedEntityFactory factory, string key, string source)
    {
        Factory = factory;
        Key = key;
        Source = source;
    }

    public SeedEntityFactory Factory { get; }

    public SeedReferenceResolver Resolver => Factory.Resolver;

    /// <summary>
    /// Key of the record being created. For child records "{parentKey}/{childKey}".
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Subpath of the file the record comes from.
    /// </summary>
    public string Source { get; }
}

/// <summary>
/// Creates entities from merged seed records.
/// </summary>
/// <remarks>
/// Fields bound by the descriptor (references, child collections, seed-only fields) are handled by their
/// handlers. All remaining fields are deserialized strictly: only writable scalar properties of the entity
/// (no navigation properties, no "Id") are accepted, unknown fields throw, enums are written as names.
/// Created top-level entities are registered in the <see cref="SeedReferenceResolver"/> under their key,
/// child entities under "{parentKey}/{childKey}".
/// </remarks>
public sealed class SeedEntityFactory
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        NumberHandling = JsonNumberHandling.Strict,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { ScalarEntityPropertiesOnly }
        },
        Converters =
        {
            new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false)
        }
    };

    private static readonly HashSet<Type> ScalarTypes =
    [
        typeof(string), typeof(decimal), typeof(DateTime), typeof(DateTimeOffset),
        typeof(Guid), typeof(TimeSpan), typeof(DateOnly), typeof(TimeOnly)
    ];

    public SeedEntityFactory(SeedReferenceResolver resolver)
    {
        Resolver = Guard.NotNull(resolver);
    }

    public SeedReferenceResolver Resolver { get; }

    /// <summary>
    /// Creates entities for all records of a record set, registers them and the set's aliases in the resolver.
    /// </summary>
    public List<T> CreateAll<T>(SeedRecordSet recordSet) where T : BaseEntity, new()
    {
        Guard.NotNull(recordSet);

        if (recordSet.Descriptor is not SeedEntityDescriptor<T> descriptor)
        {
            throw new ArgumentException($"The record set belongs to '{recordSet.Descriptor.Name}', not to '{typeof(T).Name}'.", nameof(recordSet));
        }

        Resolver.AddAliases<T>(recordSet.Aliases);

        return recordSet.Records.Select(x => Create(x, descriptor)).ToList();
    }

    /// <summary>
    /// Creates the entity for a single record and registers it in the resolver.
    /// </summary>
    public T Create<T>(SeedRecord record, SeedEntityDescriptor<T> descriptor) where T : BaseEntity, new()
    {
        Guard.NotNull(record);
        Guard.NotNull(descriptor);

        var entity = CreateEntity(record.Data, descriptor, new SeedBindingContext(this, record.Key, record.Source));
        Resolver.Add(record.Key, entity);

        return entity;
    }

    /// <summary>
    /// Creates a child entity (e.g. a state of a country) and registers it as "{parentKey}/{childKey}".
    /// </summary>
    public TChild CreateChild<TChild>(JsonObject data, SeedEntityDescriptor<TChild> descriptor, SeedBindingContext parent)
        where TChild : BaseEntity, new()
    {
        Guard.NotNull(data);
        Guard.NotNull(descriptor);
        Guard.NotNull(parent);

        var childKey = SeedJson.GetKey(data, descriptor.KeyField, $"{parent.Source} → {parent.Key}")
            ?? throw new SeedDataFormatException($"{parent.Source} → {parent.Key}: {descriptor.Name} record without key field '{descriptor.KeyField}'.");

        var key = $"{parent.Key}/{childKey}";
        var entity = CreateEntity(data, descriptor, new SeedBindingContext(this, key, parent.Source));
        Resolver.Add(key, entity);

        return entity;
    }

    private static T CreateEntity<T>(JsonObject data, SeedEntityDescriptor<T> descriptor, SeedBindingContext context)
        where T : BaseEntity, new()
    {
        var path = $"{context.Source} → {descriptor.Name} '{context.Key}'";
        var json = (JsonObject)data.DeepClone();

        // A synthetic key field (e.g. "key") identifies the record only and is not an entity property.
        if (!SerializerOptions.GetTypeInfo(typeof(T)).Properties.Any(x => x.Name.EqualsNoCase(descriptor.KeyField)))
        {
            SeedJson.TryRemove(json, descriptor.KeyField, out _);
        }

        // Take out all fields that are handled by the descriptor.
        var handled = new List<(string Field, Action<T, JsonNode, SeedBindingContext> Handler, JsonNode Value)>();
        foreach (var (field, handler) in descriptor.FieldHandlers)
        {
            if (SeedJson.TryRemove(json, field, out var value) && value != null)
            {
                handled.Add((field, handler, value));
            }
        }

        T entity;
        try
        {
            entity = json.Deserialize<T>(SerializerOptions);
        }
        catch (JsonException ex)
        {
            throw new SeedDataFormatException($"{path}: {ex.Message}", ex);
        }

        foreach (var (field, handler, value) in handled)
        {
            try
            {
                handler(entity, value, context);
            }
            catch (Exception ex) when (ex is SeedDataFormatException or InvalidOperationException or FormatException)
            {
                throw new SeedDataFormatException($"{path} → {field}: {ex.Message}", ex);
            }
        }

        return entity;
    }

    private static void ScalarEntityPropertiesOnly(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object || !typeof(BaseEntity).IsAssignableFrom(typeInfo.Type))
        {
            return;
        }

        for (var i = typeInfo.Properties.Count - 1; i >= 0; i--)
        {
            var property = typeInfo.Properties[i];
            if (property.Set == null || property.Name == nameof(BaseEntity.Id) || !IsScalar(property.PropertyType))
            {
                typeInfo.Properties.RemoveAt(i);
            }
        }
    }

    private static bool IsScalar(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive || type.IsEnum || ScalarTypes.Contains(type);
    }
}
