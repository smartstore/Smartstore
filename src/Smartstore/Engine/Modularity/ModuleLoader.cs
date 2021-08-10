using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyModel;

namespace Smartstore.Engine.Modularity
{
    internal class ModuleLoader
    {
        private readonly IApplicationContext _appContext;

        public ModuleLoader(IApplicationContext appContext)
        {
            _appContext = appContext;
        }

        public void LoadModule(ModuleDescriptor descriptor)
        {
            if (descriptor == null)
            {
                return;
            }
            
            var assemblyPath = descriptor.FileProvider.MapPath(descriptor.AssemblyName);
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);



            var assemblies = new[] { Assembly.GetEntryAssembly(), assembly };
            foreach (var asm in assemblies)
            {
                var hset = new HashSet<string>();
                var dependencyContext = DependencyContext.Load(asm);
                if (dependencyContext != null)
                {
                    foreach (var library in dependencyContext.CompileLibraries)
                    {
                        try
                        {
                            var refPaths = library.ResolveReferencePaths().ToArray();
                            hset.AddRange(refPaths);
                        }
                        catch (Exception ex)
                        {
                            var ex2 = ex;
                        }
                    }
                }
            }


            var assemblyInfo = new ModuleAssemblyInfo(descriptor)
            {
                Assembly = assembly,
                Installed = true
            };

            descriptor.Module = assemblyInfo;
        }
    }
}
