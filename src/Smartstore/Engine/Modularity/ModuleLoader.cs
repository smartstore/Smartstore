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

            var assemblyInfo = new ModuleAssemblyInfo(descriptor)
            {
                Assembly = assembly,
                Installed = true
            };

            descriptor.Module = assemblyInfo;
        }
    }
}
