using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autofac;
using FluentMigrator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Data;
using Smartstore.Data;
using Smartstore.Data.Caching;
using Smartstore.Data.Providers;
using Smartstore.Domain;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Engine.Modularity;
using Smartstore.Templating;
using Smartstore.Templating.Liquid;

namespace Smartstore.Core.Bootstrapping
{
    internal class CoreStarter : StarterBase
    {
        public override int Order => (int)StarterOrdering.Early;

        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            RegisterTypeConverters();

            // CodePages dependency required by ExcelDataReader to avoid NotSupportedException "No data is available for encoding 1252."
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (!appContext.IsInstalled)
            {
                services.AddSingleton<IDbContextFactory<SmartDbContext>>(
                    new SimpleDbContextFactory<SmartDbContext>(appContext.AppConfiguration.DbMigrationCommandTimeout));
            }
            else
            {
                // Application DbContext as pooled factory
                services.AddPooledDbContextFactory<SmartDbContext>((c, builder) =>
                {
                    builder
                        .UseSecondLevelCache()
                        .UseDbFactory(b => 
                        {
                            // Add all core models from Smartstore.Core assembly
                            b.AddModelAssembly(typeof(SmartDbContext).Assembly);

                            var moduleAssemblies = appContext.ModuleCatalog.GetInstalledModules()
                                .Select(x => x.Module.Assembly)
                                .Where(x => x.GetLoadableTypes().Any(IsDbModelCandidate))
                                .Distinct();

                            // Add all module assemblies containing domain entities or migrations
                            b.AddModelAssemblies(moduleAssemblies);

                            // Add provider specific entity configurations
                            b.AddModelAssembly(DataSettings.Instance.DbFactory.GetType().Assembly);
                        });
                }, appContext.AppConfiguration.DbContextPoolSize);

                services.AddDbQuerySettings();
            }

            services.AddDbMigrator(appContext);

            services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<SmartDbContext>>().CreateDbContext());
        }

        private static bool IsDbModelCandidate(Type type)
        {
            if (typeof(BaseEntity).IsAssignableFrom(type) || typeof(IMigration).IsAssignableFrom(type))
            {
                return !type.IsAbstract && !type.IsInterface;
            }

            return false;
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

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            builder.RegisterType<LiquidTemplateEngine>().As<ITemplateEngine>().SingleInstance();
            
            builder.RegisterModule(new LoggingModule());
            builder.RegisterModule(new PackagingModule());
            builder.RegisterModule(new LocalizationModule());
            builder.RegisterModule(new CommonServicesModule());
            builder.RegisterModule(new DbHooksModule(appContext));
            builder.RegisterModule(new StoresModule());
        }
    }
}
