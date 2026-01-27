#nullable enable

using System.Diagnostics;
using Smartstore.Domain;

namespace Smartstore.Collections;

/// <summary>
/// Manages data keys like <see cref="BaseEntity.Id"/> and offers a combination of eager and lazy data loading.
/// <para>
/// This type is intended to be used as a scoped, single-consumer instance. It is not thread-safe.
/// </para>
/// </summary>
public class LazyMultimap<T> : Multimap<int, T>
{
    private readonly Func<int[], Task<Multimap<int, T>>> _load;

    /// <summary>
    /// Data keys like <see cref="BaseEntity.Id"/> whose data have already been loaded.
    /// It is also used to avoid database round trips with empty results.
    /// </summary>
    private readonly HashSet<int> _loaded;

    /// <summary>
    /// Collected data keys like <see cref="BaseEntity.Id"/> whose data have not yet been loaded.
    /// </summary>
    private readonly HashSet<int> _collect;
    //private int _roundTripCount;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="load"><para>int[]</para> keys like <see cref="BaseEntity.Id"/>, <para>Multimap{int, T}></para> delegate to load data.</param>
    /// <param name="collect">Keys of eager loaded data.</param>
    public LazyMultimap(Func<int[], Task<Multimap<int, T>>> load, IEnumerable<int>? collect = null)
    {
        Guard.NotNull(load);

        _load = load;
        _loaded = [];
        _collect = collect == null ? [] : [.. collect];
    }

    /// <summary>
    /// Gets a value indicating whether all data has been loaded.
    /// </summary>
    public bool FullyLoaded { get; private set; }

    /// <summary>
    /// Collect keys for later (lazy) combined loading.
    /// Data keys are collected internally in order to load the associated data in one go using <see cref="GetOrLoadAsync(int)"/> or <see cref="GetOrLoad(int)"/>.
    /// </summary>
    /// <param name="keys">Data keys like <see cref="BaseEntity.Id"/>.</param>
    public virtual void Collect(IEnumerable<int>? keys)
    {
        if (keys?.Any() ?? false)
        {
            //_collect = _collect.Union(keys).ToList();
            _collect.UnionWith(keys);
        }
    }

    /// <summary>
    /// Collect single key for later (lazy) combined loading.
    /// Data keys are collected internally in order to load the associated data in one go using <see cref="GetOrLoadAsync(int)"/> or <see cref="GetOrLoad(int)"/>.
    /// </summary>
    /// <param name="key">Data key like <see cref="BaseEntity.Id"/>.</param>
    public virtual void Collect(int key)
    {
        if (key != 0 && !_collect.Contains(key))
        {
            _collect.Add(key);
        }
    }

    /// <summary>
    /// Clears all internally collected data and data keys.
    /// </summary>
    public override void Clear()
    {
        _loaded.Clear();
        _collect.Clear();
        FullyLoaded = false;
        //_roundTripCount = 0;

        base.Clear();
    }

    /// <summary>
    /// Ensures that all data is loaded and returns the data associated with <paramref name="key"/>.
    /// </summary>
    /// <param name="key">Data key like <see cref="BaseEntity.Id"/>.</param>
    /// <returns>Data associated with <paramref name="key"/>.</returns>
    public virtual async Task<ICollection<T>> GetOrLoadAsync(int key)
    {
        if (key == 0)
        {
            return [];
        }

        if (!_loaded.Contains(key))
        {
            await LoadAsync([key]);
        }

        // Better not override indexer cause of stack overflow risk.
        var result = base[key];

        Debug.Assert(_loaded.Contains(key), $"Possible missing multimap result for key {key} and type {typeof(T).Name}.", string.Empty);

        return result!;
    }

    /// <summary>
    /// Ensures that all data is loaded and returns the data associated with <paramref name="key"/>.
    /// </summary>
    /// <param name="key">Data key like <see cref="BaseEntity.Id"/>.</param>
    /// <returns>Data associated with <paramref name="key"/>.</returns>
    public virtual ICollection<T> GetOrLoad(int key)
    {
        return GetOrLoadAsync(key).Await();
    }

    /// <summary>
    /// Immediately loads all data.
    /// </summary>
    public async Task LoadAllAsync()
    {
        await LoadAsync(_collect);
        FullyLoaded = true;
    }

    /// <summary>
    /// Immediately loads all data.
    /// </summary>
    public void LoadAll()
    {
        LoadAsync(_collect).Await();
        FullyLoaded = true;
    }

    /// <summary>
    /// Main method that loads all data that have not yet been loaded.
    /// </summary>
    /// <param name="keys">Data keys like <see cref="BaseEntity.Id"/>.</param>
    protected virtual async Task LoadAsync(IEnumerable<int>? keys)
    {
        if (keys == null)
        {
            return;
        }

        // Collect candidates in one pass: (keys U _collect) \ _loaded
        HashSet<int>? candidates = null;

        foreach (var key in keys)
        {
            if (key == 0 || _loaded.Contains(key))
            {
                continue;
            }

            candidates ??= new HashSet<int>();
            candidates.Add(key);
        }

        // Add collected keys as well (and exclude already-loaded).
        if (_collect.Count != 0)
        {
            candidates ??= new HashSet<int>(_collect.Count);

            foreach (var key in _collect)
            {
                if (key == 0 || _loaded.Contains(key))
                {
                    continue;
                }

                candidates.Add(key);
            }
        }

        // Invalidate, do not load again.
        _collect.Clear();

        if (candidates == null || candidates.Count == 0)
        {
            return;
        }

        var loadKeys = candidates.ToArray();

        //++_roundTripCount;
        //Debug.WriteLine("Round trip {0} of {1}: {2}", _roundTripCount, typeof(T).Name, string.Join(",", loadKeys.OrderBy(x => x)));

        var items = await _load(loadKeys);

        _loaded.AddRange(loadKeys);

        if (items != null)
        {
            foreach (var range in items)
            {
                base.AddRange(range.Key, range.Value);
            }
        }
    }
}
