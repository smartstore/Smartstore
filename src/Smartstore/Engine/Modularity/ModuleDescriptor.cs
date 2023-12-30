using System.Text;
using System.Xml;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using Smartstore.Collections;
using Smartstore.IO;
using Smartstore.Utilities;
using IOPath = System.IO.Path;

namespace Smartstore.Engine.Modularity
{
    /// <inheritdoc/>
    public class ModuleDescriptor : IModuleDescriptor, ITopologicSortable<string>, IComparable<ModuleDescriptor>
    {
        private string _assemblyName;
        private string _resourceRootKey;
        private string _brandImageFileName;
        private IFileProvider _webFileProvider;

        #region Create

        /// <summary>
        /// Creates a module descriptor.
        /// </summary>
        /// <param name="directory">The module directory.</param>
        /// <param name="root">The <see cref="IFileSystem"/> instance used to enumrate the module directories.</param>
        /// <returns>The descriptor instance or <c>null</c> if directory does not exist or does not contain a 'module.json' file.</returns>
        public static ModuleDescriptor Create(IDirectory directory, IFileSystem root)
        {
            Guard.NotNull(directory, nameof(directory));

            if (!directory.Exists)
            {
                return null;
            }

            var manifestFile = directory.GetFile("module.json");
            if (!manifestFile.Exists)
            {
                //throw new FileNotFoundException("File 'module.json' not found.", manifestFile.PhysicalPath);
                return null;
            }

            var descriptor = ParseManifest(manifestFile.ReadAllText());

            if (descriptor.SystemName != directory.Name)
            {
                descriptor.SystemName = directory.Name;
            }

            if (!SmartstoreVersion.IsAssumedCompatible(descriptor.MinAppVersion))
            {
                descriptor.Incompatible = true;
            }

            if (!IsKnownGroup(descriptor.Group))
            {
                descriptor.Group = "Misc";
            }

            descriptor.Path = "/Modules/" + directory.Name + "/";
            descriptor.PhysicalPath = directory.PhysicalPath;

            if (CommonHelper.IsDevEnvironment)
            {
                // Try to point file provider to source code directory in dev mode.
                var sourceRoot = IOPath.GetFullPath(IOPath.Combine(CommonHelper.ContentRoot.Root, @"..\Smartstore.Modules"));
                var dirNamesToCheck = new[] { directory.Name, directory.Name + "-sym" };

                foreach (var name in dirNamesToCheck)
                {
                    var dir = new DirectoryInfo(IOPath.Combine(sourceRoot, name));
                    if (dir.Exists)
                    {
                        var linkTarget = dir.ResolveLinkTarget(true);
                        descriptor.SourcePhysicalPath = linkTarget?.FullName ?? dir.FullName;

                        break;
                    }
                }
            }

            descriptor.ContentRoot = descriptor.SourcePhysicalPath != null
                ? new LocalFileSystem(descriptor.SourcePhysicalPath)
                : new ExpandedFileSystem(directory.Name, root);

            return descriptor;
        }

        public static ModuleDescriptor ParseManifest(string manifestJson)
        {
            Guard.NotEmpty(manifestJson, nameof(manifestJson));
            return JsonConvert.DeserializeObject<ModuleDescriptor>(manifestJson);
        }

        #endregion

        #region GroupComparer

        internal readonly static string[] KnownGroups = new string[]
        {
            "Admin",
            "Marketing",
            "Payment",
            "Shipping",
            "Tax",
            "Analytics",
            "CMS",
            "Media",
            "SEO",
            "Data",
            "Globalization",
            "Api",
            "Mobile",
            "Social",
            "Security",
            "Developer",
            "Sales",
            "Design",
            "Performance",
            "B2B",
            "Storefront",
            "Law"
        };
        public readonly static IComparer<string> KnownGroupComparer = new GroupComparer();

        class GroupComparer : Comparer<string>
        {
            public override int Compare(string x, string y) => Array.FindIndex(KnownGroups, s => s == x) - Array.FindIndex(KnownGroups, s => s == y);
        }

        private static bool IsKnownGroup(string group)
        {
            if (group.IsEmpty())
            {
                return false;
            }

            return KnownGroups.Contains(group, StringComparer.OrdinalIgnoreCase);
        }

        #endregion

        #region IExtensionDescriptor

        /// <inheritdoc/>
        ExtensionType IExtensionDescriptor.ExtensionType
            => ExtensionType.Module;

        /// <inheritdoc/>
        string IExtensionDescriptor.Name
            => SystemName;

        /// <inheritdoc/>
        [JsonProperty]
        public string FriendlyName { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty]
        public string Description { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty]
        public string Group { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty]
        public string Author { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty]
        public string ProjectUrl { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty]
        public string Tags { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty]
        public Version Version { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty]
        public Version MinAppVersion { get; internal set; }

        #endregion

        #region IExtensionLocation

        /// <inheritdoc/>
        public string Path { get; internal set; }

        /// <inheritdoc/>
        /// <remarks>
        /// Setter is public for testing purposes only. Please don't set a value.
        /// </remarks>
        public string PhysicalPath { get; set; }

        /// <inheritdoc/>
        public IFileSystem ContentRoot { get; internal set; }

        /// <inheritdoc/>
        public IFileProvider WebRoot
        {
            get => _webFileProvider ??= new ExpandedFileSystem("wwwroot", ContentRoot);
            internal set => _webFileProvider = Guard.NotNull(value, nameof(value));
        }

        #endregion

        /// <inheritdoc/>
        [JsonProperty]
        public string SystemName { get; internal set; }

        string ITopologicSortable<string>.Key
            => SystemName;


        /// <inheritdoc/>
        [JsonProperty]
        public string[] DependsOn { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty]
        public int Order { get; internal set; }

        /// <inheritdoc/>
        public bool Incompatible { get; internal set; }

        /// <inheritdoc/>
        public string AssemblyName
        {
            get => _assemblyName ??= SystemName.EnsureEndsWith(".dll");
            set => _assemblyName = value;
        }

        /// <inheritdoc/>
        public string SourcePhysicalPath { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty]
        public string Theme { get; internal set; }

        /// <inheritdoc/>
        public ModuleAssemblyInfo Module { get; internal set; }

        /// <inheritdoc/>
        [JsonProperty]
        public string ResourceRootKey
        {
            get => _resourceRootKey ??= DiscoverResourceRootKey();
            internal set => _resourceRootKey = value;
        }

        private string DiscoverResourceRootKey()
        {
            if (ContentRoot == null)
            {
                return null;
            }

            try
            {
                // Try to get root-key from first entry of XML file
                var localizationDir = ContentRoot.GetDirectory("Localization");

                if (localizationDir.Exists)
                {
                    var localizationFile = localizationDir.EnumerateFiles("*.xml").FirstOrDefault();
                    if (localizationFile != null)
                    {
                        XmlDocument doc = new();
                        doc.Load(localizationFile.PhysicalPath);
                        var key = doc.SelectSingleNode(@"//Language/LocaleResource")?.Attributes["Name"]?.InnerText;
                        if (key.HasValue() && key.Contains('.'))
                        {
                            return key[..key.LastIndexOf('.')];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Dump();
            }

            return string.Empty;
        }

        /// <inheritdoc/>
        [JsonProperty]
        public string BrandImageFileName
        {
            get => _brandImageFileName ??= DiscoverBrandImageFileName();
            internal set => _brandImageFileName = value;
        }

        private string DiscoverBrandImageFileName()
        {
            if (WebRoot == null)
            {
                return null;
            }

            var filesToCheck = new[] { "branding.png", "branding.gif", "branding.jpg", "branding.jpeg" };
            foreach (var file in filesToCheck)
            {
                var fileInfo = WebRoot.GetFileInfo(file);
                if (fileInfo.Exists)
                {
                    return file;
                }
            }

            return string.Empty;
        }

        private readonly static CompositeFormat _formatSettingKey = CompositeFormat.Parse("PluginSetting.{0}.{1}");
        /// <inheritdoc/>
        public string GetSettingKey(string name)
        {
            // Compat: DON'T change Plugin > Module
            return _formatSettingKey.FormatCurrent(SystemName, name);
        }

        public override string ToString()
            => FriendlyName;

        public override bool Equals(object obj)
        {
            return obj is ModuleDescriptor other &&
                SystemName != null &&
                SystemName.EqualsNoCase(other.SystemName);
        }

        public override int GetHashCode()
            => SystemName.GetHashCode();

        public int CompareTo(ModuleDescriptor other)
        {
            if (Order != other.Order)
            {
                return Order.CompareTo(other.Order);
            }
            else if (FriendlyName != null)
            {
                return FriendlyName.CompareTo(other.FriendlyName);
            }

            return 0;
        }
    }
}