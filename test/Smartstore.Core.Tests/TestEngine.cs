using System.Collections.Generic;
using System.Reflection;
using Smartstore.Engine;

namespace Smartstore.Core.Tests
{
    public class TestEngine : IEngine
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

        class EngineStarter : EngineStarter<TestEngine>
        {
            public EngineStarter(TestEngine engine)
                : base(engine)
            {
            }

            protected override IEnumerable<Assembly> ResolveCoreAssemblies()
            {
                return new[] { typeof(IEngine).Assembly/*, typeof(SmartDbContext).Assembly*/ };
            }
        }
    }
}
