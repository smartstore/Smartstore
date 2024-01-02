using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Smartstore.Core.Theming;

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
        /// <param name="route">The path/route of the bundle to return.</param>
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
        private readonly Dictionary<string, Bundle> _bundles = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Bundle> _staticBundles = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DynamicBundle> _dynamicBundles = new(StringComparer.OrdinalIgnoreCase);

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IApplicationContext _appContext;
        private readonly IThemeRegistry _themeRegistry;
        private readonly IOptionsMonitor<BundlingOptions> _options;

        public BundleCollection(
            IHttpContextAccessor httpContextAccessor,
            IApplicationContext appContext,
            IThemeRegistry themeRegistry,
            IOptionsMonitor<BundlingOptions> options)
        {
            _httpContextAccessor = httpContextAccessor;
            _appContext = appContext;
            _themeRegistry = themeRegistry;
            _options = options;
        }

        public IReadOnlyList<Bundle> Bundles
            => _bundles.Values.AsReadOnly();

        public Bundle GetBundleFor(string route)
        {
            Guard.NotEmpty(route, nameof(route));

            route = Bundle.NormalizeRoute(route);

            if (route.HasValue())
            {
                if (_staticBundles.TryGetValue(route, out var bundle))
                {
                    return bundle;
                }

                if (_dynamicBundles.Count > 0)
                {
                    foreach (var dynamicBundle in _dynamicBundles.Values)
                    {
                        if (dynamicBundle.TryMatchRoute(route, out var routeValues))
                        {
                            var dynamicBundleConext = new DynamicBundleContext
                            {
                                Path = route,
                                RouteValues = routeValues,
                                Bundle = dynamicBundle,
                                HttpContext = _httpContextAccessor.HttpContext,
                                ApplicationContext = _appContext,
                                ThemeRegistry = _themeRegistry,
                                BundlingOptions = _options.CurrentValue
                            };

                            if (dynamicBundle.IsStatisfiedByConstraints(dynamicBundleConext))
                            {
                                return new DynamicBundleMatch(dynamicBundleConext);
                            }
                        }
                    }
                }
            }

            return null;
        }

        public void Add(Bundle bundle)
        {
            Guard.NotNull(bundle);

            if (bundle is DynamicBundle dynamicBundle)
            {
                _dynamicBundles[dynamicBundle.Route] = dynamicBundle;
            }
            else
            {
                if (!bundle.SourceFiles.Any())
                {
                    throw new ArgumentException($"The bundle '{bundle.Route}' must contain at least one source file.", nameof(bundle));
                }

                _staticBundles[bundle.Route] = bundle;
            }

            _bundles[bundle.Route] = bundle;
        }

        public bool Remove(Bundle bundle)
        {
            Guard.NotNull(bundle);

            var wasRemoved = _bundles.TryRemove(bundle.Route, out _);
            if (wasRemoved)
            {
                if (bundle is DynamicBundle dynamicBundle)
                {
                    _dynamicBundles.TryRemove(dynamicBundle.Route, out _);
                }
                else
                {
                    _staticBundles.TryRemove(bundle.Route, out _);
                }
            }

            return wasRemoved;
        }

        public void Clear()
        {
            _bundles.Clear();
            _dynamicBundles.Clear();
        }
    }
}
