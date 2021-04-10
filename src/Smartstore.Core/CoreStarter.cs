using System.Collections.Generic;
using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.DependencyInjection;
using Smartstore.Data;
using Smartstore.Engine;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Bootstrapping
{
    internal class CoreStarter : StarterBase
    {
        public override int Order => (int)StarterOrdering.Early;

        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext, bool isActiveModule)
        {
            var appConfig = appContext.AppConfiguration;

            RegisterTypeConverters();
            services.AddDbMigrator();

            if (appContext.IsInstalled)
            {
                // Application DbContext as pooled factory
                services.AddPooledApplicationDbContextFactory<SmartDbContext>(
                    DataSettings.Instance.DbFactory.SmartDbContextType,
                    appContext.AppConfiguration.DbContextPoolSize,
                    optionsBuilder: (c, o, rel) => 
                    {
                        // TODO: (core) Why does WithMigrationsHistoryTableName() not work?
                        rel.WithMigrationsHistoryTableName("__EFMigrationsHistory_Core");
                    });

                // TODO: (core) Move this as extension to OptionBuilder (e.g. o.IsMigratable())
                DbMigrationManager.Instance.RegisterMigratableDbContext(typeof(SmartDbContext));

                services.AddDbQuerySettings();

                services.AddMiniProfiler(o =>
                {
                    // TODO: (more) Move MiniProfiler start to module and configure
                }).AddEntityFramework();
            }
        }

        internal static void RegisterTypeConverters()
        {
            // Internal for testing

            ITypeConverter converter = new ShippingOptionConverter(true);
            TypeConverterFactory.RegisterConverter<IList<ShippingOption>>(converter);
            TypeConverterFactory.RegisterConverter<List<ShippingOption>>(converter);
            TypeConverterFactory.RegisterConverter<ShippingOption>(new ShippingOptionConverter(false));

            converter = new ProductBundleItemOrderDataConverter(true);
            TypeConverterFactory.RegisterConverter<IList<ProductBundleItemOrderData>>(converter);
            TypeConverterFactory.RegisterConverter<List<ProductBundleItemOrderData>>(converter);
            TypeConverterFactory.RegisterConverter<ProductBundleItemOrderData>(new ProductBundleItemOrderDataConverter(false));

            TypeConverterFactory.RegisterListConverter<GiftCardCouponCode>(new GiftCardCouponCodeConverter());
        }

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            builder.RegisterModule(new LoggingModule());
            builder.RegisterModule(new ProvidersModule(appContext));
            builder.RegisterModule(new LocalizationModule());
            builder.RegisterModule(new CommonServicesModule());

            if (appContext.IsInstalled)
            {
                builder.RegisterModule(new DbHooksModule(appContext));
                builder.RegisterModule(new StoresModule());
            }
        }
    }
}
