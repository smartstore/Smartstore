using System;
using Autofac;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Rendering;

namespace Smartstore.Core.Bootstrapping
{
    public class RuleModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
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
        }
    }
}
