using System;
using System.Collections.Generic;
using System.Linq;

namespace Smartstore.Engine.Modularity
{
    internal class ModuleExplorer
    {
        private readonly IApplicationContext _appContext;

        public ModuleExplorer(IApplicationContext appContext)
        {
            _appContext = appContext;
        }

        public IEnumerable<ModuleDescriptor> DiscoverModules()
        {
            var allDirectories = _appContext.ModulesRoot.EnumerateDirectories()
                .Where(d => d.Name != "bin" && d.Name != "_Backup")
                .OrderBy(d => d.Name)
                .ToArray();

            var modules = allDirectories
                .AsParallel()
                .AsOrdered()
                .Select(d => ModuleDescriptor.Create(d))
                .Where(x => x != null)
                .ToArray();

            Array.Sort(modules);
            return modules;
        }
    }
}
