using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Smartstore.Engine.Modularity.ApplicationParts
{
    public static class ApplicationPartManagerExtensions
    {
        public static void PopulateModules(this ApplicationPartManager partManager, IModuleCatalog moduleCatalog)
        {
            Guard.NotNull(partManager, nameof(partManager));
            Guard.NotNull(moduleCatalog, nameof(moduleCatalog));

            foreach (var descriptor in moduleCatalog.GetInstalledModules())
            {
                if (descriptor.Module.Assembly == null)
                {
                    continue;
                }
                
                PopulateModuleParts(partManager, descriptor);
            }
        }

        private static void PopulateModuleParts(ApplicationPartManager partManager, IModuleDescriptor descriptor)
        {
            // First add module entry assembly
            partManager.ApplicationParts.Add(new ModulePart(descriptor));

            // Resolve related assemblies and other parts (e.g. compiled views assembly).
            var assemblies = GetModulePartAssemblies(descriptor);

            var seenAssemblies = new HashSet<Assembly>();

            foreach (var assembly in assemblies)
            {
                if (!seenAssemblies.Add(assembly))
                {
                    continue;
                }

                var partFactory = ApplicationPartFactory.GetApplicationPartFactory(assembly);
                foreach (var applicationPart in partFactory.GetApplicationParts(assembly))
                {
                    partManager.ApplicationParts.Add(applicationPart);
                }
            }
        }

        private static IEnumerable<Assembly> GetModulePartAssemblies(IModuleDescriptor descriptor)
        {
            var assembly = descriptor.Module.Assembly;
            
            // Use ApplicationPartAttribute to get the closure of direct or transitive dependencies that reference MVC.
            var assembliesFromAttributes = assembly.GetCustomAttributes<ApplicationPartAttribute>()
                .Select(name => Assembly.Load(name.AssemblyName))
                .OrderBy(assembly => assembly.FullName, StringComparer.Ordinal)
                .SelectMany(GetAssemblyClosure);

            // The SDK will not include the entry assembly as an application part. We'll explicitly list it
            // and have it appear before all other assemblies \ ApplicationParts.
            return GetAssemblyClosure(assembly)
                .Concat(assembliesFromAttributes);
        }

        private static IEnumerable<Assembly> GetAssemblyClosure(Assembly assembly)
        {
            var relatedAssemblies = RelatedAssemblyAttribute.GetRelatedAssemblies(assembly, throwOnError: false)
                .OrderBy(assembly => assembly.FullName, StringComparer.Ordinal);

            foreach (var relatedAssembly in relatedAssemblies)
            {
                yield return relatedAssembly;
            }
        }
    }
}
