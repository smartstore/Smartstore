using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Smartstore.Web.Bundling
{
    /// <summary>
    /// Contains and manages the set of registered <see cref="Bundle"/> objects.
    /// </summary>
    public interface IBundleCollection
    {
        /// <summary>
        /// Returns a bundle in the collection using the specified route.
        /// </summary>
        /// <param name="route">The route of the bundle to return.</param>
        /// <returns>The bundle for the route or null if no bundle exists at the path.</returns>
        Bundle GetBundleFor(string route);

        /// <summary>
        /// Gets all bundles in the collection as readonly list.
        /// </summary>
        IReadOnlyList<Bundle> Bundles { get; }

        /// <summary>
        /// Adds a bundle to the collection. Will overwrite any existing item.
        /// </summary>
        /// <param name="bundle">The bundle to add.</param>
        void Add(Bundle bundle);

        /// <summary>
        /// Removes a bundle from the collection.
        /// </summary>
        /// <param name="bundle">The bundle to remove.</param>
        /// <returns><c>true</c> if the bundle was removed; otherwise, <c>false</c>.</returns>
        bool Remove(Bundle bundle);

        /// <summary>
        /// Removes all bundles from the collection.
        /// </summary>
        void Clear();
    }

    internal class BundleCollection : IBundleCollection
    {
        private readonly ConcurrentDictionary<string, Bundle> _bundles = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<Bundle> Bundles
            => _bundles.Values.AsReadOnly();

        public Bundle GetBundleFor(string route)
        {
            Guard.NotEmpty(route, nameof(route));

            if (_bundles.TryGetValue(Bundle.NormalizeRoute(route), out var bundle))
            {
                return bundle;
            }

            return null;
        }

        public void Add(Bundle bundle)
        {
            Guard.NotNull(bundle, nameof(bundle));

            if (!bundle.SourceFiles.Any())
            {
                throw new ArgumentException($"The bundle '{bundle.Route}' must contain at least one source file.", nameof(bundle));
            }

            _bundles[bundle.Route] = bundle;
        }

        public bool Remove(Bundle bundle)
        {
            return _bundles.TryRemove(bundle.Route, out _);
        }

        public void Clear()
        {
            _bundles.Clear();
        }
    }
}
