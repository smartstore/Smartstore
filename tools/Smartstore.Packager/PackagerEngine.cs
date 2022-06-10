using System.Reflection;
using Smartstore.Engine;

namespace Smartstore.Packager
{
    internal class PackagerEngine : IEngine
    {
        public IApplicationContext Application { get; set; }
        public ScopedServiceContainer Scope { get; set; }
        public bool IsStarted { get; set; }
        public bool IsInitialized { get; set; }

        public IEngineStarter Start(IApplicationContext application)
        {
            Guard.NotNull(application, nameof(application));

            Application = application;

            return new EngineStarter(this);
        }

        class EngineStarter : EngineStarter<PackagerEngine>
        {
            public EngineStarter(PackagerEngine engine)
                : base(engine)
            {
            }

            protected override IEnumerable<Assembly> ResolveCoreAssemblies()
            {
                return new[] { typeof(IEngine).Assembly, typeof(Program).Assembly };
            }
        }
    }
}
