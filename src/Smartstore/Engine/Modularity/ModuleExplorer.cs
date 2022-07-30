using Smartstore.Collections;

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
            if (_appContext.ModulesRoot == null)
            {
                return Enumerable.Empty<ModuleDescriptor>();
            }

            var allDirectories = _appContext.ModulesRoot.EnumerateDirectories()
                .Where(d => d.Name != "bin" && d.Name != "_Backup")
                //.OrderBy(d => d.Name)
                .ToArray();

            var modules = allDirectories
                //.AsParallel()
                //.AsOrdered()
                .Select(d => ModuleDescriptor.Create(d, _appContext.ModulesRoot))
                .Where(x => x != null)
                .ToArray()
                .SortTopological()
                .Cast<ModuleDescriptor>();

            return modules;
        }
    }
}
