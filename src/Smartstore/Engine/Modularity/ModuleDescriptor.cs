using System;
using Microsoft.Extensions.FileProviders;
using Smartstore.IO;

namespace Smartstore.Engine.Modularity
{
    /// <inheritdoc/>
    public class ModuleDescriptor : IModuleDescriptor
    {
        private string _assemblyName;
        private IFileSystem _fileProvider;
        private string _resourceRootKey;

        #region Static

        public static ModuleDescriptor Parse(string manifestJson)
        {
            throw new NotImplementedException();
        }

        public static ModuleDescriptor Parse(IFile file)
        {
            throw new NotImplementedException();
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
        public IFileProvider WebFileProvider { get; internal set; }

        #endregion

        /// <inheritdoc/>
        public string SystemName { get; internal set; }

        /// <inheritdoc/>
        public int Order { get; internal set; }

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
        public ModuleAssemblyInfo AssemblyInfo { get; internal set; }

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
    }
}