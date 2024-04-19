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
                return [];
            }

            // Try to find the smaller ref file in the "ref" subfolder and use its path (if it exists).
            var location = assembly.Location;
            var file = new FileInfo(location);
            var refLocation = Path.Combine(file.DirectoryName, "ref", file.Name);

            if (File.Exists(refLocation))
            {
                location = refLocation;
            }

            var privateAssemblyPaths = Descriptor.Module.LoadContext.Assemblies.Select(x => x.Location);
            return (new[] { location }).Concat(privateAssemblyPaths);
        }
    }
}
