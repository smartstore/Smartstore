using System;
using Microsoft.Extensions.FileProviders;
using Smartstore.IO;

namespace Smartstore.Engine.Modularity
{
    public class ModuleDescriptor : IExtensionDescriptor, IExtensionLocation
    {
        #region IExtensionDescriptor

        string IExtensionDescriptor.Name 
            => SystemName;

        ExtensionType IExtensionDescriptor.ExtensionType 
            => ExtensionType.Module;

        /// <inheritdoc/>
        public string FriendlyName { get; init; }

        /// <inheritdoc/>
        public string Description { get; init; }

        /// <inheritdoc/>
        public string Group { get; init; }

        /// <inheritdoc/>
        public string Author { get; init; }

        /// <inheritdoc/>
        public string WebSite { get; init; }

        /// <inheritdoc/>
        public string Tags { get; init; }

        /// <inheritdoc/>
        public SemanticVersion Version { get; init; }

        /// <inheritdoc/>
        public SemanticVersion MinAppVersion { get; init; }

        #endregion

        #region IExtensionLocation

        /// <inheritdoc/>
        public string Path { get; init; }

        /// <inheritdoc/>
        public string PhysicalPath { get; init; }

        /// <inheritdoc/>
        public IFileProvider WebFileProvider { get; protected internal set; }

        #endregion

        private string _resourceRootKey;

        /// <summary>
        /// Gets or sets the system name
        /// </summary>
        public string SystemName { get; init; }

        /// <summary>
        /// Module installer runtime type.
        /// </summary>
        public Type ModuleClrType { get; set; }

        /// <summary>
        /// Gets or sets the (display) order
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the module is installed
        /// </summary>
        public bool Installed { get; set; }

        /// <summary>
        /// Gets a value indicating whether the module is incompatible with the current application version
        /// </summary>
        public bool Incompatible { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether the module is configurable
        /// </summary>
        /// <remarks>
        /// A module is configurable when it implements the <see cref="IConfigurable"/> interface
        /// </remarks>
        public bool IsConfigurable { get; set; }

        /// <summary>
        /// Gets the file provider that references the module's root directory.
        /// </summary>
        public IFileSystem FileProvider { get; protected internal set; }

        /// <summary>
        /// Gets or sets the root key of string resources.
        /// </summary>
        /// <remarks>
        /// Tries to get it from first entry of resource XML file if not specified.
        /// In that case the first resource name should not contain a dot if it's not part of the root key.
        /// Otherwise you get the wrong root key.
        /// </remarks>
        public string ResourceRootKey
        {
            // TODO: (core) Impl ModuleDescriptor.ResourceRootKey getter
            get => "Smartstore.PseudoModule";
            set => _resourceRootKey = value;
        }

        /// <summary>
        /// Builds a setting key. Pattern: "PluginSetting.{ModuleSystemName}.{SettingName}"
        /// </summary>
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
    }
}