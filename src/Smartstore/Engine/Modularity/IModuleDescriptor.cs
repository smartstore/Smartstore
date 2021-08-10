using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.IO;

namespace Smartstore.Engine.Modularity
{
    /// <summary>
    /// Provides metadata about a module.
    /// </summary>
    public interface IModuleDescriptor : IExtensionDescriptor, IExtensionLocation
    {
        /// <summary>
        /// Gets the system name
        /// </summary>
        string SystemName { get; }

        /// <summary>
        /// Gets the (display) order
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Gets a value indicating whether the module is incompatible with the current application version.
        /// </summary>
        bool Incompatible { get; }

        /// <summary>
        /// Gets the file provider that references the module's root directory.
        /// </summary>
        IFileSystem FileProvider { get; }

        /// <summary>
        /// Gets the root key of string resources.
        /// </summary>
        /// <remarks>
        /// Tries to get it from first entry of resource XML file if not specified.
        /// In that case the first resource name should not contain a dot if it's not part of the root key.
        /// Otherwise you get the wrong root key.
        /// </remarks>
        string ResourceRootKey { get; }

        /// <summary>
        /// Builds a setting key. Pattern: "PluginSetting.{ModuleSystemName}.{SettingName}"
        /// </summary>
        string GetSettingKey(string name);

        /// <summary>
        /// Gets the main assembly file name.
        /// </summary>
        /// <remarks>
        string AssemblyName { get; }

        /// <summary>
        /// Gets the module's runtime assembly info.
        /// </summary>
        ModuleAssemblyInfo Module { get; }
    }
}
