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
            builder.RegisterType<MeasureService>().As<IMeasureService>().InstancePerLifetimeScope();
            builder.RegisterType<WebHelper>().As<IWebHelper>().InstancePerLifetimeScope();

            builder.RegisterType<CommonServices>().As<ICommonServices>().InstancePerLifetimeScope();
        }
    }
}