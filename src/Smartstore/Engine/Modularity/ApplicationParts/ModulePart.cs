using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Smartstore.Engine.Modularity.ApplicationParts
{
    /// <summary>
    /// A custom ApplicationPart implementation that will NOT raise an exception
    /// when resolving module main assembly path.
    /// </summary>
    public class ModulePart : ModuleRazorAssemblyPart, IApplicationPartTypeProvider, ICompilationReferencesProvider
    {
        public ModulePart(IModuleDescriptor descriptor)
            : base(descriptor?.Module?.Assembly)
        {
            Descriptor = descriptor;
        }

        public IModuleDescriptor Descriptor { get; }

        public override string Name 
            => Descriptor.Name;

        public IEnumerable<TypeInfo> Types 
            => Descriptor.Module.Assembly.DefinedTypes;

        public IEnumerable<string> GetReferencePaths()
        {
            var assembly = Descriptor.Module.Assembly;

            if (assembly.IsDynamic)
            {
                // Skip loading process for dynamic assemblies. This prevents DependencyContextLoader from reading the
                // .deps.json file from either manifest resources or the assembly location, which will fail.
                return Enumerable.Empty<string>();
            }

            return (new[] { assembly.Location }).Concat(Descriptor.Module.PrivateAssemblies);
        }
    }
}
