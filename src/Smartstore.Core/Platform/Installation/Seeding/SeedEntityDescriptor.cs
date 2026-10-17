using System.Text.Json.Nodes;

namespace Smartstore.Core.Installation.Seeding;

/// <summary>
/// Describes where the seed records of an entity type live and how they are keyed and merged.
/// </summary>
public abstract class SeedEntityDescriptor
{
    private readonly List<string> _files = [];

    protected SeedEntityDescriptor(Type entityType, string keyField)
    {
        EntityType = Guard.NotNull(entityType);
        Spec = new SeedCollectionSpec(keyField);
    }

    /// <summary>
    /// The entity type name. Used in messages and as reference type name.
    /// </summary>
    public string Name => EntityType.Name;

    public Type EntityType { get; }

    /// <summary>
    /// The JSON field that identifies a record, e.g. "sku".
    /// </summary>
    public string KeyField => Spec.KeyField;

    /// <summary>
    /// Merge specification (key field and nested child collections).
    /// </summary>
    public SeedCollectionSpec Spec { get; }

    /// <summary>
    /// Base file paths relative to the "base" folder, e.g. "core/currencies.json".
    /// The last path segment may contain wildcards, e.g. "samples/catalog/products/*.json".
    /// Matching files are read in ordinal order of their names.
    /// </summary>
    public IReadOnlyList<string> Files => _files;

    /// <summary>
    /// Optional defaults file relative to the "base" folder. Its values apply to every record
    /// before the record's own values.
    /// </summary>
    public string DefaultsFile { get; protected set; }

    /// <summary>
    /// Optional transformation of each record after defaults and before the culture overlay,
    /// e.g. culture-wide translations that a record overlay can still override.
    /// </summary>
    public Action<JsonObject> BeforeOverlay { get; set; }

    protected void AddFiles(string[] files)
    {
        foreach (var file in files)
        {
            Guard.NotEmpty(file);
            _files.Add(file.Replace('\\', '/'));
        }
    }
}

/// <summary>
/// Describes the seed records of <typeparamref name="T"/>: their files, key, references to other
/// seed records, nested child collections and seed-only fields that need custom handling.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public sealed class SeedEntityDescriptor<T> : SeedEntityDescriptor
    where T : BaseEntity, new()
{
    private readonly List<(string Field, Action<T, JsonNode, SeedBindingContext> Handler)> _fieldHandlers = [];

    /// <param name="keyField">The JSON field that identifies a record, e.g. "sku".</param>
    public SeedEntityDescriptor(string keyField)
        : base(typeof(T), keyField)
    {
    }

    /// <summary>
    /// Fields handled by the factory itself instead of plain deserialization, in registration order.
    /// </summary>
    internal IReadOnlyList<(string Field, Action<T, JsonNode, SeedBindingContext> Handler)> FieldHandlers => _fieldHandlers;

    /// <summary>
    /// Called after an entity has been created and all fields have been bound.
    /// </summary>
    internal Action<T, SeedBindingContext> CreatedHandler { get; private set; }

    /// <summary>
    /// Sets values that are not seed data, e.g. GUIDs or timestamps of the installation.
    /// Called after an entity has been created and all fields have been bound.
    /// </summary>
    public SeedEntityDescriptor<T> OnCreated(Action<T, SeedBindingContext> action)
    {
        Guard.NotNull(action);

        var previous = CreatedHandler;
        CreatedHandler = previous == null ? action : (entity, context) => { previous(entity, context); action(entity, context); };
        return this;
    }

    /// <summary>
    /// Adds base files relative to the "base" folder. The last segment may contain wildcards.
    /// </summary>
    public SeedEntityDescriptor<T> FromFiles(params string[] files)
    {
        AddFiles(Guard.NotNull(files));
        return this;
    }

    /// <summary>
    /// Sets the defaults file relative to the "base" folder.
    /// </summary>
    public SeedEntityDescriptor<T> WithDefaults(string file)
    {
        Guard.NotEmpty(file);
        DefaultsFile = file.Replace('\\', '/');
        return this;
    }

    /// <summary>
    /// Declares a field that holds the key of another seed record, e.g. <c>"taxCategory": "standard"</c>.
    /// </summary>
    /// <param name="field">JSON field name.</param>
    /// <param name="assign">Assigns the resolved entity, e.g. <c>(x, t) => x.TaxCategory = t</c>.</param>
    public SeedEntityDescriptor<T> Reference<TTarget>(string field, Action<T, TTarget> assign)
        where TTarget : BaseEntity
    {
        Guard.NotNull(assign);

        return Bind(field, (entity, node, context) =>
        {
            var key = SeedJson.GetKeyValue(node, field);
            assign(entity, context.Resolver.Get<TTarget>(key));
        });
    }

    /// <summary>
    /// Declares a field that holds a list of keys of other seed records, e.g. <c>"categories": ["golf"]</c>.
    /// </summary>
    /// <param name="field">JSON field name.</param>
    /// <param name="assign">Assigns the resolved entities in the order of the keys.</param>
    public SeedEntityDescriptor<T> References<TTarget>(string field, Action<T, IReadOnlyList<TTarget>> assign)
        where TTarget : BaseEntity
    {
        Guard.NotNull(assign);

        return Bind(field, (entity, node, context) =>
        {
            if (node is not JsonArray array)
            {
                throw new SeedDataFormatException($"Field '{field}' must be an array of keys.");
            }

            var targets = array
                .Select(x => context.Resolver.Get<TTarget>(SeedJson.GetKeyValue(x, field)))
                .ToList();

            assign(entity, targets);
        });
    }

    /// <summary>
    /// Declares a nested child collection, e.g. the states of a country. Children are merged by their own key
    /// and are registered for reference resolution as "{parentKey}/{childKey}".
    /// </summary>
    /// <param name="field">JSON field name.</param>
    /// <param name="collection">Gets the entity collection to add the children to.</param>
    /// <param name="child">Descriptor of the child records. Its files are ignored.</param>
    public SeedEntityDescriptor<T> Child<TChild>(string field, Func<T, ICollection<TChild>> collection, SeedEntityDescriptor<TChild> child)
        where TChild : BaseEntity, new()
    {
        Guard.NotNull(collection);
        Guard.NotNull(child);

        Spec.WithChild(field, child.Spec);

        return Bind(field, (entity, node, context) =>
        {
            if (node is not JsonArray array)
            {
                throw new SeedDataFormatException($"Child collection '{field}' must be an array.");
            }

            var target = collection(entity);

            foreach (var item in array)
            {
                if (item is not JsonObject obj)
                {
                    throw new SeedDataFormatException($"Child collection '{field}' must contain objects only.");
                }

                target.Add(context.Factory.CreateChild(obj, child, context));
            }
        });
    }

    /// <summary>
    /// Declares a nested list of child records without keys, e.g. specification attribute assignments.
    /// The children are not registered for reference resolution, and an overlay replaces the whole list.
    /// </summary>
    /// <param name="field">JSON field name.</param>
    /// <param name="collection">Gets the entity collection to add the children to.</param>
    /// <param name="child">Descriptor of the child records. Its files and key are ignored.</param>
    public SeedEntityDescriptor<T> ChildList<TChild>(string field, Func<T, ICollection<TChild>> collection, SeedEntityDescriptor<TChild> child)
        where TChild : BaseEntity, new()
    {
        Guard.NotNull(collection);
        Guard.NotNull(child);

        return Bind(field, (entity, node, context) =>
        {
            if (node is not JsonArray array)
            {
                throw new SeedDataFormatException($"Child list '{field}' must be an array.");
            }

            var target = collection(entity);

            for (var i = 0; i < array.Count; i++)
            {
                if (array[i] is not JsonObject obj)
                {
                    throw new SeedDataFormatException($"Child list '{field}' must contain objects only.");
                }

                target.Add(context.Factory.CreateListItem(obj, child, context, field, i));
            }
        });
    }

    /// <summary>
    /// Declares a seed-only field with custom handling, e.g. a list of picture paths. The field is not deserialized.
    /// The binder is not called when the field is missing or null.
    /// </summary>
    public SeedEntityDescriptor<T> Bind(string field, Action<T, JsonNode, SeedBindingContext> binder)
    {
        Guard.NotEmpty(field);
        Guard.NotNull(binder);

        if (_fieldHandlers.Any(x => x.Field.EqualsNoCase(field)))
        {
            throw new InvalidOperationException($"Field '{field}' of '{Name}' is already bound.");
        }

        _fieldHandlers.Add((field, binder));
        return this;
    }
}
