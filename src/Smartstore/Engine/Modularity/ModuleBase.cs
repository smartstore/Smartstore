using System;
using System.Threading.Tasks;

namespace Smartstore.Engine.Modularity
{
    public abstract class ModuleBase : IModule
    {
        /// <inheritdoc />
        public virtual IModuleDescriptor Descriptor { get; set; }

        /// <inheritdoc />
        public virtual Task InstallAsync()
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public virtual Task UninstallAsync()
        {
            return Task.CompletedTask;
        }
    }
}
