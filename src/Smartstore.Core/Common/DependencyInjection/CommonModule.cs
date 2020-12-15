using Autofac;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Web;
using Smartstore.Web;

namespace Smartstore.Core.Common.DependencyInjection
{
    public sealed class CommonModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<CurrencyService>().As<ICurrencyService>().InstancePerLifetimeScope();
            builder.RegisterType<DateTimeHelper>().As<IDateTimeHelper>().InstancePerLifetimeScope();
            builder.RegisterType<DeliveryTimeService>().As<IDeliveryTimeService>().InstancePerLifetimeScope();
            builder.RegisterType<MeasureService>().As<IMeasureService>().InstancePerLifetimeScope();
            builder.RegisterType<GenericAttributeService>().As<IGenericAttributeService>().InstancePerLifetimeScope();
            builder.RegisterType<WebHelper>().As<IWebHelper>().InstancePerLifetimeScope();
            builder.RegisterType<CommonServices>().As<ICommonServices>().InstancePerLifetimeScope();
        }
    }
}