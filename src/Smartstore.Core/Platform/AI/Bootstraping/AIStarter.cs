using Autofac;
using Smartstore.Core.Platform.AI;
using Smartstore.Core.Platform.AI.Prompting;
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
                var messageCreatorTypes = appContext.TypeScanner.FindTypes<IAIMessageCreator>();
                foreach (var type in messageCreatorTypes)
                {
                    builder.RegisterType(type).As<IAIMessageCreator>().Keyed<IAIMessageCreator>(type).InstancePerLifetimeScope();
                }

                builder.RegisterType<AIMessageBuilder>().AsSelf().InstancePerLifetimeScope();
                builder.RegisterType<AIMessageResources>().AsSelf().InstancePerLifetimeScope();
                builder.RegisterType<AIProviderFactory>().As<IAIProviderFactory>().InstancePerLifetimeScope();
            }
        }
    }
}