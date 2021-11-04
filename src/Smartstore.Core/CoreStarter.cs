using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autofac;
using FluentMigrator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Smartstore.Bootstrapping;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
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
            // Type converters
            RegisterTypeConverters();

            // Default Json serializer settings
            JsonConvert.DefaultSettings = () =>  
            {
                var settings = new JsonSerializerSettings
                {
                    ContractResolver = SmartContractResolver.Instance,
                    TypeNameHandling = TypeNameHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    ObjectCreationHandling = ObjectCreationHandling.Auto,
                    NullValueHandling = NullValueHandling.Ignore,
                    MaxDepth = 32
                };

                return settings;
            };

            // CodePages dependency required by ExcelDataReader to avoid NotSupportedException "No data is available for encoding 1252."
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            services.AddDbQuerySettings();
            services.AddDbMigrator(appContext);
            services.AddWkHtmlToPdf();

            // Application DbContext as pooled factory
            services.AddPooledDbContextFactory<SmartDbContext>((c, builder) =>
            {
                if (appContext.IsInstalled)
                {
                    builder.UseSecondLevelCache();
                }
                    
                builder
                    .UseDbFactory(b => 
                    {
                        b.AddModelAssemblies(new[]
                        { 
                            // Add all core models from Smartstore.Core assembly
                            typeof(SmartDbContext).Assembly,
                            // Add provider specific entity configurations
                            DataSettings.Instance.DbFactory.GetType().Assembly
                        });

                        if (appContext.IsInstalled)
                        {
                            b.AddDataSeeder<SmartDbContext, SmartDbContextDataSeeder>();
                        }
                    });

                var configurers = c.GetServices<IDbContextConfigurationSource<SmartDbContext>>();
                foreach (var configurer in configurers)
                {
                    configurer.Configure(c, builder);
                }

            }, appContext.AppConfiguration.DbContextPoolSize);

            services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<SmartDbContext>>().CreateDbContext());
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
