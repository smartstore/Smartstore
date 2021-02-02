using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Smartstore.Collections
{
    /// <summary>
    /// Manages data keys and offers a combination of eager and lazy data loading.
    /// </summary>
    public class LazyMultimap<T> : Multimap<int, T>
	{
		private readonly Func<int[], Task<Multimap<int, T>>> _load;
		private readonly HashSet<int> _loaded;  // Avoids database round trips with empty results.
		private readonly HashSet<int> _collect;
		//private int _roundTripCount;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="load"><para>int[]</para> keys like Entity.Id, <para>Multimap{int, T}></para> delegate to load data.</param>
		/// <param name="collect">Keys of eager loaded data.</param>
		public LazyMultimap(Func<int[], Task<Multimap<int, T>>> load, IEnumerable<int> collect = null)
		{
			Guard.NotNull(load, nameof(load));

			_load = load;
			_loaded = new HashSet<int>();
			_collect = collect == null ? new HashSet<int>() : new HashSet<int>(collect);
		}

		public bool FullyLoaded { get; private set; }

		/// <summary>
		/// Collect keys for combined loading.
		/// </summary>
		/// <param name="keys">Data keys</param>
		[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
		public virtual void Collect(IEnumerable<int> keys)
		{
			if (keys?.Any() ?? false)
			{
				//_collect = _collect.Union(keys).ToList();
				_collect.UnionWith(keys);
			}
		}

		/// <summary>
		/// Collect single key for combined loading.
		/// </summary>
		/// <param name="key">Data key</param>
		public virtual void Collect(int key)
		{
			if (key != 0 && !_collect.Contains(key))
			{
				_collect.Add(key);
			}
		}

		public override void Clear()
		{
			_loaded.Clear();
			_collect.Clear();
			FullyLoaded = false;
			//_roundTripCount = 0;

			base.Clear();
		}

		/// <summary>
		/// Gets data. Loads it if not already loaded yet.
		/// </summary>
		/// <param name="key">Data key.</param>
		/// <returns>Collection of data.</returns>
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

			// better not override indexer cause of stack overflow risk
			var result = base[key];

			Debug.Assert(_loaded.Contains(key), $"Possible missing multimap result for key {key} and type {typeof(T).Name}.", string.Empty);

			return result;
		}

		/// <summary>
		/// Gets data. Loads it if not already loaded yet.
		/// </summary>
		/// <param name="key">Data key.</param>
		/// <returns>Collection of data.</returns>
		public virtual ICollection<T> GetOrLoad(int key)
		{
			if (key == 0)
			{
				return new List<T>();
			}

			if (!_loaded.Contains(key))
			{
				LoadAsync(new int[] { key }).Await();
			}

			// better not override indexer cause of stack overflow risk
			var result = base[key];

			Debug.Assert(_loaded.Contains(key), $"Possible missing multimap result for key {key} and type {typeof(T).Name}.", string.Empty);

			return result;
		}

		public async Task LoadAllAsync()
		{
			await LoadAsync(_collect);
			FullyLoaded = true;
		}

		public void LoadAll()
		{
			LoadAsync(_collect).Await();
			FullyLoaded = true;
		}

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
