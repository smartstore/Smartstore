using Autofac;
using Smartstore.Core.Messages;
using Smartstore.Engine;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Bootstrapping
{
    public sealed class MessagingStarter : StarterBase
    {
        public override bool Matches(IApplicationContext appContext)
            => appContext.IsInstalled;

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            builder.RegisterType<EmailAccountService>().As<IEmailAccountService>().InstancePerLifetimeScope();
            builder.RegisterType<QueuedEmailService>().As<IQueuedEmailService>().InstancePerLifetimeScope();
            builder.RegisterType<NewsletterSubscriptionService>().As<INewsletterSubscriptionService>().InstancePerLifetimeScope();
            builder.RegisterType<MessageFactory>().As<IMessageFactory>().InstancePerLifetimeScope();
            builder.RegisterType<MessageModelProvider>().As<IMessageModelProvider>().InstancePerLifetimeScope();
            builder.RegisterType<CampaignService>().As<ICampaignService>().InstancePerLifetimeScope();
        }
    }
}