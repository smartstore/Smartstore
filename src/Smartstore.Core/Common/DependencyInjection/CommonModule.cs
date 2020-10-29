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
            builder.RegisterType<IMeasureService>().As<MeasureService>().InstancePerLifetimeScope();
            builder.RegisterType<IWebHelper>().As<WebHelper>().InstancePerLifetimeScope();

            builder.RegisterType<ICommonServices>().As<CommonServices>().InstancePerLifetimeScope();
        }
    }
}