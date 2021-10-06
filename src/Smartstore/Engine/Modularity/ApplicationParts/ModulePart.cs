using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Razor.Hosting;
using Microsoft.Extensions.DependencyModel;

namespace Smartstore.Engine.Modularity.ApplicationParts
{
    /// <summary>
    /// A custom ApplicationPart implementation that will NOT raise an exception
    /// when resolving module main assembly path.
    /// </summary>
    public class ModulePart : ApplicationPart, IApplicationPartTypeProvider, ICompilationReferencesProvider, IRazorCompiledItemProvider
    {
        public ModulePart(IModuleDescriptor descriptor)
        {
            Guard.NotNull(descriptor?.Module?.Assembly, nameof(descriptor));

            Descriptor = descriptor;
        }

        public IModuleDescriptor Descriptor { get; }

        public override string Name 
            => Descriptor.Name;

        public IEnumerable<TypeInfo> Types 
            => Descriptor.Module.Assembly.DefinedTypes;

        IEnumerable<RazorCompiledItem> IRazorCompiledItemProvider.CompiledItems
        {
            get
            {
                // Smartstore.MyModule.Views --> Smartstore.MyModule
                var moduleName = Name.Replace(".Views", string.Empty);
                var loader = new ModuleRazorCompiledItemLoader(moduleName);
                return loader.LoadItems(Descriptor.Module.Assembly);
            }
        }

        public IEnumerable<string> GetReferencePaths()
        {
            var assembly = Descriptor.Module.Assembly;

            if (assembly.IsDynamic)
            {
                // Skip loading process for dynamic assemblies. This prevents DependencyContextLoader from reading the
                // .deps.json file from either manifest resources or the assembly location, which will fail.
                return Enumerable.Empty<string>();
            }

            var dependencyContext = DependencyContext.Load(assembly);

            if (dependencyContext == null)
            {
                // If an application has been compiled without preserveCompilationContext, return the path to the assembly
                // as a reference. For runtime compilation, this will allow the compilation to succeed as long as it least
                // one application part has been compiled with preserveCompilationContext and contains a super set of types
                // required for the compilation to succeed.
                return new[] { assembly.Location };
            }
            else
            {
                // Skip the first library (the module main assembly itself), because path resolution will raise an exception...
                var libs = dependencyContext.CompileLibraries.Skip(1).SelectMany(library => library.ResolveReferencePaths());

                // ...instead prepend assembly location
                return (new[] { assembly.Location }).Concat(libs);
            }
        }
    }
}
