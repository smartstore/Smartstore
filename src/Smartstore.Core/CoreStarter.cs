using System.Collections.Generic;
using System.Text;
using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Data;
using Smartstore.Data;
using Smartstore.Data.Caching;
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

            // CodePages dependency required by ExcelDataReader to avoid NotSupportedException "No data is available for encoding 1252."
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

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
                            f.MigrationsHistoryTable(SmartDbContext.MigrationHistoryTableName);
                        });
                });

                services.AddDbMigrator(appContext);
                services.AddDbQuerySettings();
            }
        }

        internal static void RegisterTypeConverters()
        {
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
            builder.RegisterModule(new PackagingModule());
            builder.RegisterModule(new LocalizationModule());
            builder.RegisterModule(new CommonServicesModule());
            builder.RegisterModule(new DbHooksModule(appContext));
            builder.RegisterModule(new StoresModule());
        }
    }
}
