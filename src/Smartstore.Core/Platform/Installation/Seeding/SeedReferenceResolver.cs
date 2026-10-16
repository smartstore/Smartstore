namespace Smartstore.Core.Installation.Seeding;

/// <summary>
/// Resolves keys of seed records to their entities. Keys are compared case-insensitively.
/// Aliases are resolved with a single hop: a key that is not registered is looked up in the aliases.
/// </summary>
public sealed class SeedReferenceResolver
{
    private readonly Dictionary<Type, Dictionary<string, BaseEntity>> _entities = [];
    private readonly Dictionary<Type, Dictionary<string, string>> _aliases = [];

    /// <summary>
    /// Registers an entity under its key.
    /// </summary>
    public void Add<T>(string key, T entity) where T : BaseEntity
    {
        Guard.NotEmpty(key);
        Guard.NotNull(entity);

        var map = GetMap(_entities, typeof(T));
        if (!map.TryAdd(key, entity))
        {
            throw new SeedDataFormatException($"Duplicate {typeof(T).Name} key '{key}'.");
        }
    }

    /// <summary>
    /// Registers key aliases for <typeparamref name="T"/>, e.g. "apparel" → "standard".
    /// </summary>
    public void AddAliases<T>(IEnumerable<KeyValuePair<string, string>> aliases) where T : BaseEntity
        => AddAliases(typeof(T), aliases);

    /// <inheritdoc cref="AddAliases{T}(IEnumerable{KeyValuePair{string, string}})"/>
    public void AddAliases(Type entityType, IEnumerable<KeyValuePair<string, string>> aliases)
    {
        Guard.NotNull(entityType);
        Guard.NotNull(aliases);

        var map = GetMap(_aliases, entityType);
        foreach (var (alias, target) in aliases)
        {
            map[alias] = target;
        }
    }

    public bool TryGet<T>(string key, out T entity) where T : BaseEntity
    {
        entity = null;

        if (key.IsEmpty() || !_entities.TryGetValue(typeof(T), out var map))
        {
            return false;
        }

        if (!map.TryGetValue(key, out var found)
            && _aliases.TryGetValue(typeof(T), out var aliases)
            && aliases.TryGetValue(key, out var target))
        {
            map.TryGetValue(target, out found);
        }

        entity = (T)found;
        return entity != null;
    }

    /// <summary>
    /// Gets the entity registered under <paramref name="key"/> or one of its aliases.
    /// </summary>
    /// <exception cref="SeedDataFormatException">The key cannot be resolved.</exception>
    public T Get<T>(string key) where T : BaseEntity
    {
        if (TryGet<T>(key, out var entity))
        {
            return entity;
        }

        throw new SeedDataFormatException($"Unknown {typeof(T).Name} key '{key}'.");
    }

    /// <summary>
    /// Gets the ID of the entity registered under <paramref name="key"/>. The entity must have been saved.
    /// </summary>
    /// <exception cref="SeedDataFormatException">The key cannot be resolved or the entity is not saved yet.</exception>
    public int GetId<T>(string key) where T : BaseEntity
    {
        var entity = Get<T>(key);
        if (entity.Id == 0)
        {
            throw new SeedDataFormatException($"{typeof(T).Name} '{key}' has not been saved yet, so its ID is unknown.");
        }

        return entity.Id;
    }

    private static Dictionary<string, TValue> GetMap<TValue>(Dictionary<Type, Dictionary<string, TValue>> maps, Type type)
    {
        if (!maps.TryGetValue(type, out var map))
        {
            map = new Dictionary<string, TValue>(StringComparer.OrdinalIgnoreCase);
            maps[type] = map;
        }

        return map;
    }
}
