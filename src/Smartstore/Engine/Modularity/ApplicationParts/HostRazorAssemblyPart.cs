using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Razor.Hosting;

namespace Smartstore.Engine.Modularity.ApplicationParts
{
    /// <summary>
    /// An <see cref="ApplicationPart"/> for compiled host/web Razor assemblies.
    /// </summary>
    public class HostRazorAssemblyPart : ApplicationPart, IRazorCompiledItemProvider, IApplicationPartTypeProvider
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
    }
}
