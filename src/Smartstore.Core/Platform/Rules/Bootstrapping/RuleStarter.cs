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

            // Rendering.
            builder.RegisterType<RuleTemplateSelector>().As<IRuleTemplateSelector>().InstancePerLifetimeScope();

            // Register provider resolver delegate.
            builder.Register<Func<RuleScope, IRuleProvider>>(c =>
            {
                // TODO: register providers explicitly
                var cc = c.Resolve<IComponentContext>();
                return key => cc.ResolveKeyed<IRuleProvider>(key);
            });

            // Rule options provider.
            builder.RegisterType<RuleSetRuleOptionsProvider>().As<IRuleOptionsProvider>().InstancePerLifetimeScope();
        }
    }
}
