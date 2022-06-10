using System.Diagnostics;
using Smartstore.Domain;

namespace Smartstore.Collections
{
    /// <summary>
    /// Manages data keys like <see cref="BaseEntity.Id"/> and offers a combination of eager and lazy data loading.
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
        public LazyMultimap(Func<int[], Task<Multimap<int, T>>> load, IEnumerable<int> collect = null)
        {
            Guard.NotNull(load, nameof(load));

            _load = load;
            _loaded = new HashSet<int>();
            _collect = collect == null ? new HashSet<int>() : new HashSet<int>(collect);
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
        public virtual void Collect(IEnumerable<int> keys)
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
                return new List<T>();
            }

            if (!_loaded.Contains(key))
            {
                await LoadAsync(new int[] { key });
            }

            // Better not override indexer cause of stack overflow risk.
            var result = base[key];

            Debug.Assert(_loaded.Contains(key), $"Possible missing multimap result for key {key} and type {typeof(T).Name}.", string.Empty);

            return result;
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
        protected virtual async Task LoadAsync(IEnumerable<int> keys)
        {
            if (keys == null)
            {
                return;
            }

            var loadKeys = (_collect.Count == 0 ? keys : _collect.Concat(keys))
                .Distinct()
                .Except(_loaded)
                .ToArray();

            // Invalidate, do not load again.
            _collect.Clear();

            if (loadKeys.Any())
            {
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
    }
}
