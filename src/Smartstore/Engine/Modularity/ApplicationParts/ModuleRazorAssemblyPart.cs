using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Razor.Hosting;

namespace Smartstore.Engine.Modularity.ApplicationParts
{
    /// <summary>
    /// An <see cref="ApplicationPart"/> for compiled module Razor assemblies.
    /// The view identifier/path of <see cref="RazorCompiledItem"/> will contain
    /// the modules's path as prefix, so that view resolution can succeed without 
    /// starting the Razor runtime compiler (which consumes a lot of memory).
    /// </summary>
    public class ModuleRazorAssemblyPart : ApplicationPart, IRazorCompiledItemProvider
    {
        public ModuleRazorAssemblyPart(Assembly assembly)
        {
            Assembly = Guard.NotNull(assembly);
        }

        public Assembly Assembly { get; }

        public override string Name
            => Assembly.GetName().Name;

        IEnumerable<RazorCompiledItem> IRazorCompiledItemProvider.CompiledItems
        {
            get
            {
                // Smartstore.MyModule.Views --> Smartstore.MyModule
                var moduleName = Name.Replace(".Views", string.Empty);
                var loader = new ModuleRazorCompiledItemLoader(moduleName);
                return loader.LoadItems(Assembly);
            }
        }
    }
}
