using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Smartstore.Engine.Modularity.ApplicationParts
{
    /// <summary>
    /// Configures the host assembly as a <see cref="HostRazorAssemblyPartFactory"/>.
    /// </summary>
    public class HostRazorAssemblyPartFactory : ApplicationPartFactory
    {
        public override IEnumerable<ApplicationPart> GetApplicationParts(Assembly assembly)
        {
            yield return new HostRazorAssemblyPart(assembly);
        }
    }
}
