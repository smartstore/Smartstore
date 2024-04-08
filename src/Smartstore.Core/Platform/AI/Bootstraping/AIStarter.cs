using Autofac;
using Smartstore.AI.Services;
using Smartstore.Core.AI;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Bootstrapping
{
    internal class AIStarter : StarterBase
    {
        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            if (appContext.IsInstalled)
            {
                // Register all prompt generators.
                var promptGeneratorTypes = appContext.TypeScanner.FindTypes<IPromptGenerator>();
                foreach (var type in promptGeneratorTypes)
                {
                    builder.RegisterType(type).As<IPromptGenerator>().Keyed<IPromptGenerator>(type).InstancePerLifetimeScope();
                }

                builder.RegisterType<PromptHelper>().InstancePerLifetimeScope();                
            }
        }
    }
}
