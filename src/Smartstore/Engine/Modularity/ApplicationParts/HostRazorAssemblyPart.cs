using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Razor.Hosting;
using Microsoft.Extensions.DependencyModel;

namespace Smartstore.Engine.Modularity.ApplicationParts
{
    /// <summary>
    /// An <see cref="ApplicationPart"/> for compiled host/web Razor assemblies.
    /// </summary>
    public class HostRazorAssemblyPart : ApplicationPart, IRazorCompiledItemProvider, IApplicationPartTypeProvider, ICompilationReferencesProvider
    {
        public HostRazorAssemblyPart(Assembly assembly)
        {
            Assembly = Guard.NotNull(assembly);
        }

        public Assembly Assembly { get; }

        public override string Name
            => Assembly.GetName().Name;

        public IEnumerable<TypeInfo> Types 
            => Assembly.DefinedTypes;

        IEnumerable<RazorCompiledItem> IRazorCompiledItemProvider.CompiledItems
        {
            get
            {
                var loader = new HostRazorCompiledItemLoader();
                return loader.LoadItems(Assembly);
            }
        }

        IEnumerable<string> ICompilationReferencesProvider.GetReferencePaths()
        {
            if (Assembly.IsDynamic)
            {
                // Skip loading process for dynamic assemblies. This prevents DependencyContextLoader from reading the
                // .deps.json file from either manifest resources or the assembly location, which will fail.
                return [];
            }

            var dependencyContext = DependencyContext.Load(Assembly);
            if (dependencyContext != null)
            {
                var paths = dependencyContext.CompileLibraries.SelectMany(library => library.ResolveReferencePaths()).ToArray();
                return paths;
            }

            // If an application has been compiled without preserveCompilationContext, return the path to the assembly
            // as a reference. For runtime compilation, this will allow the compilation to succeed as long as it least
            // one application part has been compiled with preserveCompilationContext and contains a super set of types
            // required for the compilation to succeed.
            return [Assembly.Location];
        }
    }
}
