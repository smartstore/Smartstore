using System.IO.Hashing;
using System.Text;
using Newtonsoft.Json;
using Smartstore.Collections;
using Smartstore.IO;
using Smartstore.Utilities;

namespace Smartstore.Core.Content.Media.Icons
{
    public interface IIconExplorer
    {
        ICollection<IconDescription> All { get; }
        Multimap<string, string> SearchMap { get; }
        IconDescription GetIconByName(string name);
        IEnumerable<IconDescription> FindIcons(string searchTerm, bool relaxed = false);
    }

    public class IconExplorer : IIconExplorer
    {
        private IDictionary<string, IconDescription> _icons;
        private readonly Multimap<string, string> _searchMap = new(StringComparer.OrdinalIgnoreCase, x => new HashSet<string>(x));
        private readonly object _lock = new();

        private readonly IApplicationContext _appContext;

        public IconExplorer(IApplicationContext appContext)
        {
            _appContext = appContext;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        private void EnsureIsLoaded()
        {
            if (_icons == null)
            {
                lock (_lock)
                {
                    if (_icons == null)
                    {
                        try
                        {
                            _icons = LoadIconsMetadata();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                            _icons = new Dictionary<string, IconDescription>(StringComparer.OrdinalIgnoreCase);
                        }

                        foreach (var kvp in _icons)
                        {
                            var key = kvp.Key;
                            var value = kvp.Value;

                            value.Name = key;

                            // Styles
                            var styles = value.Styles;
                            if (styles != null)
                            {
                                if (styles.Length == 1 && styles[0] == "brands")
                                {
                                    value.IsBrandIcon = true;
                                }
                                else if (styles.Contains("regular"))
                                {
                                    value.HasRegularStyle = true;
                                }
                            }

                            if (value.SearchInfo?.Terms?.Length > 0)
                            {
                                foreach (var term in value.SearchInfo.Terms)
                                {
                                    _searchMap.Add(term, key);
                                }
                            }
                        }
                    }
                }
            }
        }

        private Dictionary<string, IconDescription> LoadIconsMetadata()
        {
            var fs = _appContext.AppDataRoot;
            var mapFile = fs.GetFile("icons.json");

            if (!mapFile.Exists)
            {
                throw new FileNotFoundException("Icons metadata file does not exist.", mapFile.PhysicalPath);
            }

            // (Perf) look up minified version of metadata file
            var hashCode = HashCodeCombiner.Start().Add(mapFile, false).CombinedHashString;
            var mapFileMin = fs.GetFile(PathUtility.Join(_appContext.GetTempDirectory().SubPath, $"icons.{hashCode}.json"));
            //var path = mapFileMin.Exists ? mapFileMin.SubPath : mapFile.SubPath;
            var file = mapFileMin.Exists ? mapFileMin : mapFile;

            var json = file.ReadAllText(Encoding.GetEncoding(1252));
            var icons = JsonConvert.DeserializeObject<Dictionary<string, IconDescription>>(json);

            if (!mapFileMin.Exists && mapFileMin.SubPath.HasValue())
            {
                // minified file did not exist: save it.
                var settings = new JsonSerializerSettings
                {
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                    Formatting = Formatting.None
                };

                json = JsonConvert.SerializeObject(icons, settings);
                fs.WriteAllText(mapFileMin.SubPath, json, Encoding.GetEncoding(1252));
            }

            return icons;
        }

        public ICollection<IconDescription> All
        {
            get
            {
                EnsureIsLoaded();
                return _icons.Values;
            }
        }

        public Multimap<string, string> SearchMap
        {
            get
            {
                EnsureIsLoaded();
                return _searchMap;
            }
        }

        public IconDescription GetIconByName(string name)
        {
            Guard.NotEmpty(name, nameof(name));
            EnsureIsLoaded();

            if (!_icons.TryGetValue(name, out var description))
            {
                description = new IconDescription
                {
                    IsPro = true,
                    Name = name,
                    Label = name,
                    Styles = new[] { "solid", "regular", "light", "duotone" },
                    HasRegularStyle = true,
                };
            }

            return description;
        }

        public IEnumerable<IconDescription> FindIcons(string searchTerm, bool relaxed = false)
        {
            Guard.NotEmpty(searchTerm, nameof(searchTerm));
            EnsureIsLoaded();

            var hasExactMatch = false;

            if (_icons.TryGetValue(searchTerm, out var description))
            {
                hasExactMatch = true;
                yield return description;
            }

            if (_searchMap.ContainsKey(searchTerm))
            {
                var names = _searchMap[searchTerm];
                foreach (var name in names)
                {
                    if (_icons.TryGetValue(name, out var description2))
                    {
                        hasExactMatch = true;
                        yield return description2;
                    }
                }
            }

            if (relaxed && !hasExactMatch)
            {
                foreach (var kvp in _icons)
                {
                    if (kvp.Key.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        yield return kvp.Value;
                    }
                }
            }
        }
    }
}