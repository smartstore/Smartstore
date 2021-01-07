using Autofac;
using Smartstore.Core.Messages;

namespace Smartstore.Core.DependencyInjection
{
    public sealed class MessagingModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<EmailAccountService>().As<IEmailAccountService>().InstancePerLifetimeScope();
            builder.RegisterType<QueuedEmailService>().As<IQueuedEmailService>().InstancePerLifetimeScope();
        }
    }
}