using Autofac;
using Smartstore.Bootstrapping;
using Smartstore.Core.DataExchange;
using Smartstore.Core.DataExchange.Export;
using Smartstore.Core.DataExchange.Import;
using Smartstore.Engine.Builders;
using Smartstore.Engine.Modularity;
using Smartstore.IO;

namespace Smartstore.Core.Bootstrapping
{
    internal class DataExchangeStarter : StarterBase
    {
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            services.AddDownloadManager();
        }

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            builder.RegisterType<ExportProfileService>().As<IExportProfileService>().InstancePerLifetimeScope();
            builder.RegisterType<ImportProfileService>().As<IImportProfileService>().InstancePerLifetimeScope();
            builder.RegisterType<DataExporter>().As<IDataExporter>().InstancePerLifetimeScope();
            builder.RegisterType<DataImporter>().As<IDataImporter>().InstancePerLifetimeScope();
            builder.RegisterType<MediaImporter>().As<IMediaImporter>().InstancePerDependency();

            builder.Register<Func<ImportEntityType, IEntityImporter>>(c =>
            {
                var cc = c.Resolve<IComponentContext>();
                return key => cc.ResolveKeyed<IEntityImporter>(key);
            });
        }

        public override void BuildPipeline(RequestPipelineBuilder builder)
        {
            builder.Configure(StarterOrdering.StaticFilesMiddleware, app =>
            {
                var appContext = builder.ApplicationContext;
                var assetFileProvider = app.ApplicationServices.GetRequiredService<IAssetFileProvider>();
                assetFileProvider.AddFileProvider("exchange/", new ExpandedFileSystem("exchange", appContext.TenantRoot, true));
            });
        }
    }
}
