using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Smartstore.Engine.Modularity.ApplicationParts
{
    /// <summary>
    /// Configures a module's compiled views assembly as a <see cref="ModuleRazorAssemblyPart"/>.
    /// </summary>
    public class ModuleRazorAssemblyPartFactory : ApplicationPartFactory
    {
        public override IEnumerable<ApplicationPart> GetApplicationParts(Assembly assembly)
        {
            yield return new ModuleRazorAssemblyPart(assembly);
        }
    }
}
