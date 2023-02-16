using System.Reflection;
using System.Runtime.Loader;

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

            var mainAssemblyPath = Path.Combine(descriptor.PhysicalPath, descriptor.AssemblyName);
            LoadContext = new ModuleAssemblyLoadContext(mainAssemblyPath);

            // Load the module main assembly to the default AssemblyLoadContext
            // so that razor runtime compilation will not fail to resolve the dependency.
            // But load private references of the module into isolated ModuleAssemblyLoadContext.
            Assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(mainAssemblyPath);
            ModuleType = Assembly.GetLoadableTypes()
                .Where(t => !t.IsInterface && t.IsClass && !t.IsAbstract)
                .FirstOrDefault(t => typeof(IModule).IsAssignableFrom(t));
        }

        /// <summary>
        /// Gets the isolated <see cref="AssemblyLoadContext"/> for this module.
        /// </summary>
        public AssemblyLoadContext LoadContext { get; }

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
