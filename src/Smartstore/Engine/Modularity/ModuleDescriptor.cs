using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using Smartstore.IO;

namespace Smartstore.Engine.Modularity
{
    /// <inheritdoc/>
    public class ModuleDescriptor : IModuleDescriptor, IComparable<ModuleDescriptor>
    {
        private string _assemblyName;
        private string _resourceRootKey;
        private IFileSystem _fileProvider;
        private IFileProvider _webFileProvider;

        #region Create

        /// <summary>
        /// Creates a module descriptor.
        /// </summary>
        /// <param name="directory">The module directory.</param>
        /// <returns>The descriptor instance or <c>null</c> if directory does not exist or does not contain a 'module.json' file.</returns>
        public static ModuleDescriptor Create(IDirectory directory)
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

            descriptor.PhysicalPath = directory.PhysicalPath;
            descriptor.Path = "/Modules/" + directory.Name + "/";

            if (!SmartstoreVersion.IsAssumedCompatible(descriptor.MinAppVersion))
            {
                descriptor.Incompatible = true;
            }

            if (!IsKnownGroup(descriptor.Group))
            {
                descriptor.Group = "Misc";
            }

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
        public string FriendlyName { get; internal set; }

        /// <inheritdoc/>
        public string Description { get; internal set; }

        /// <inheritdoc/>
        public string Group { get; internal set; }

        /// <inheritdoc/>
        public string Author { get; internal set; }

        /// <inheritdoc/>
        public string ProjectUrl { get; internal set; }

        /// <inheritdoc/>
        public string Tags { get; internal set; }

        /// <inheritdoc/>
        public Version Version { get; internal set; }

        /// <inheritdoc/>
        public Version MinAppVersion { get; internal set; }

        #endregion

        #region IExtensionLocation

        /// <inheritdoc/>
        public string Path { get; internal set; }

        /// <inheritdoc/>
        public string PhysicalPath { get; internal set; }

        /// <inheritdoc/>
        public IFileProvider WebFileProvider 
        { 
            get
            {
                if (_webFileProvider == null)
                {
                    var webRootFullPath = System.IO.Path.Combine(PhysicalPath, "wwwroot");
                    if (Directory.Exists(webRootFullPath))
                    {
                        _webFileProvider = Directory.Exists(webRootFullPath) 
                            ? new PhysicalFileProvider(webRootFullPath) 
                            : new NullFileProvider();
                    }
                }
                
                return _webFileProvider;
            }
            internal set => _webFileProvider = Guard.NotNull(value, nameof(value));
        }

        #endregion

        /// <inheritdoc/>
        public string SystemName { get; internal set; }

        /// <inheritdoc/>
        public int Order { get; internal set; }

        /// <inheritdoc/>
        public bool Incompatible { get; internal set; }

        /// <inheritdoc/>
        public IFileSystem FileProvider
        {
            get => _fileProvider ??= new LocalFileSystem(PhysicalPath);
            internal set => _fileProvider = Guard.NotNull(value, nameof(value));
        }

        /// <inheritdoc/>
        public string AssemblyName
        {
            get => _assemblyName ??= SystemName.EnsureEndsWith(".dll");
            set => _assemblyName = value;
        }

        /// <inheritdoc/>
        public ModuleAssemblyInfo Module { get; internal set; }

        /// <inheritdoc/>
        public string ResourceRootKey
        {
            // TODO: (core) Impl ModuleDescriptor.ResourceRootKey getter
            get => _resourceRootKey ??= "Smartstore.PseudoModule";
            set => _resourceRootKey = value;
        }

        /// <inheritdoc/>
        public string GetSettingKey(string name)
        {
            // Compat: DON'T change Plugin > Module
            return "PluginSetting.{0}.{1}".FormatWith(SystemName, name);
        }

        public override string ToString()
            => FriendlyName;

        public override bool Equals(object obj)
        {
            var other = obj as ModuleDescriptor;
            return other != null &&
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