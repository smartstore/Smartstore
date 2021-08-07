using System;
using System.Reflection;

namespace Smartstore.Engine.Modularity
{
    /// <summary>
    /// Provides runtime info about a module's assembly and its installation state.
    /// </summary>
    public class ModuleAssemblyInfo
    {
        public ModuleAssemblyInfo(IModuleDescriptor descriptor)
        {
            Descriptor = Guard.NotNull(descriptor, nameof(descriptor));
        }

        /// <summary>
        /// Gets the module descriptor.
        /// </summary>
        public IModuleDescriptor Descriptor { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the module is installed.
        /// </summary>
        public bool Installed { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether the module is configurable.
        /// </summary>
        /// <remarks>
        /// A module is configurable when it implements the <see cref="IConfigurable"/> interface
        /// </remarks>
        public bool IsConfigurable { get; init; }

        /// <summary>
        /// Gets or sets the module runtime type.
        /// </summary>
        public Type ModuleClrType { get; init; }

        /// <summary>
        /// The module main assembly.
        /// </summary>
        public Assembly Assembly { get; init; }

        /// <summary>
        /// List of assemblies found in the module folder, except the main module assembly.
        /// </summary>
        public Assembly[] ReferencedLocalAssemblies { get; init; } = Array.Empty<Assembly>();
    }
}
