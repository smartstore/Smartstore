using System;
using System.Reflection;

namespace Smartstore.Engine.Modularity
{
    /// <summary>
    /// Provides runtime info about a module's assembly and its installation state.
    /// </summary>
    public class ModuleAssemblyInfo
    {
        private bool? _isConfigurable;
        
        public ModuleAssemblyInfo(IModuleDescriptor descriptor)
        {
            Descriptor = Guard.NotNull(descriptor, nameof(descriptor));
        }

        /// <summary>
        /// Gets the module descriptor.
        /// </summary>
        public IModuleDescriptor Descriptor { get; }

        /// <summary>
        /// The module main assembly.
        /// </summary>
        public Assembly Assembly { get; init; }

        /// <summary>
        /// Gets or sets the module runtime type.
        /// </summary>
        public Type ModuleType { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether the module is configurable.
        /// </summary>
        /// <remarks>
        /// A module is configurable when it implements the <see cref="IConfigurable"/> interface
        /// </remarks>
        public bool IsConfigurable
        {
            get => _isConfigurable ??= (ModuleType != null && typeof(IConfigurable).IsAssignableFrom(ModuleType));
        }
    }
}
