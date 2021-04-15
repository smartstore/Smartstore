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
using Smartstore.Data.Caching;
using Smartstore.Data.Migrations;
using Smartstore.Data.Providers;
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

            if (appContext.IsInstalled)
            {
                var contextImplType = DataSettings.Instance.DbFactory.SmartDbContextType;
                var poolSize = appContext.AppConfiguration.DbContextPoolSize;

                // Application DbContext as pooled factory
                services.AddPooledDbContextFactory<SmartDbContext>(contextImplType, poolSize, (c, builder) =>
                {
                    builder
                        .UseSecondLevelCache()
                        .UseDbFactory(f =>
                        {
                            f.MigrationsHistoryTable("__EFMigrationsHistory_Core");
                        });
                });

                services.AddDbMigrator();
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
            builder.RegisterModule(new DbHooksModule(appContext));

            if (appContext.IsInstalled)
            {
                builder.RegisterModule(new StoresModule());
            }
        }
    }
}
