using Autofac;
using Smartstore.Core.DataExchange.Export;
using Smartstore.Core.DataExchange.Import;
using Smartstore.Engine;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Bootstrapping
{
    internal class DataExchangeStarter : StarterBase
    {
        public override bool Matches(IApplicationContext appContext)
            => appContext.IsInstalled;

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            builder.RegisterType<ExportProfileService>().As<IExportProfileService>().InstancePerLifetimeScope();
            builder.RegisterType<ImportProfileService>().As<IImportProfileService>().InstancePerLifetimeScope();
            builder.RegisterType<DataExporter>().As<IDataExporter>().InstancePerLifetimeScope();
            builder.RegisterType<DataImporter>().As<IDataImporter>().InstancePerLifetimeScope();
        }
    }
}
