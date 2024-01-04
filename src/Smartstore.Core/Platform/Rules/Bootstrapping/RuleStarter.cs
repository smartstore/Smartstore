using Autofac;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Rendering;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Bootstrapping
{
    internal class RuleStarter : StarterBase
    {
        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            builder.RegisterType<RuleService>().As<IRuleService>().InstancePerLifetimeScope();
            builder.RegisterType<RuleProviderFactory>().As<IRuleProviderFactory>().InstancePerLifetimeScope();
            builder.RegisterType<RuleSetRuleOptionsProvider>().As<IRuleOptionsProvider>().InstancePerLifetimeScope();

            // Rendering.
            builder.RegisterType<RuleTemplateSelector>().As<IRuleTemplateSelector>().InstancePerLifetimeScope();
        }
    }
}
