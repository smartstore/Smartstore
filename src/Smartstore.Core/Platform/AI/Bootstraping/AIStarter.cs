using Autofac;
using Smartstore.Core.AI;
using Smartstore.Core.AI.Prompting;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Bootstrapping
{
    internal sealed class AIStarter : StarterBase
    {
        public override bool Matches(IApplicationContext appContext) 
            => appContext.IsInstalled;

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            // Register all prompt generators.
            var promptGeneratorTypes = appContext.TypeScanner.FindTypes<IAIPromptGenerator>();
            foreach (var type in promptGeneratorTypes)
            {
                builder.RegisterType(type).As<IAIPromptGenerator>().Keyed<IAIPromptGenerator>(type).InstancePerLifetimeScope();
            }

            builder.RegisterType<DefaultAIChatCache>().As<IAIChatCache>().SingleInstance();
            builder.RegisterType<AIMessageBuilder>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<AIMessageResources>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<AIProviderFactory>().As<IAIProviderFactory>().InstancePerLifetimeScope();
        }
    }
}