using Autofac;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Bootstrapping
{
    internal sealed class LocalizationStarter : StarterBase
    {
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            services.Configure<LocalizedEntityOptions>(o => 
            {
                o.Delegates.Add(LocalizedSettingsLoader.LoadLocalizedSettings);
            });
        }

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            builder.RegisterModule(new LocalizationModule());
        }

        public override void ConfigureMvc(IMvcBuilder mvcBuilder, IServiceCollection services, IApplicationContext appContext)
        {
            mvcBuilder.AddAppLocalization();
        }
    }
}
