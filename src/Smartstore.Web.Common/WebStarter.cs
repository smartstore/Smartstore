global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Linq.Expressions;
global using System.Threading;
global using System.Threading.Tasks;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Logging.Abstractions;
global using Smartstore.Core;
global using Smartstore.Core.Data;
global using Smartstore.Core.Widgets;
global using Smartstore.Domain;
global using Smartstore.Engine;
global using EntityState = Smartstore.Data.EntityState;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders;
using Smartstore.Bootstrapping;
using Smartstore.Core.Bootstrapping;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Seo.Routing;
using Smartstore.Engine.Builders;
using Smartstore.Net;
using Smartstore.Net.Http;
using Smartstore.Utilities;
using Smartstore.Web.Bootstrapping;
using Smartstore.Web.Razor;

namespace Smartstore.Web
{
    internal class WebStarter : StarterBase
    {
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            services.AddWorkContext();
            
            if (appContext.IsInstalled)
            {
                // Configure Cookie Policy Options
                services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<CookiePolicyOptions>, CookiePolicyOptionsConfigurer>());
            }

            // Add AntiForgery
            services.AddAntiforgery(o =>
            {
                o.Cookie.Name = CookieNames.Antiforgery;
                o.HeaderName = "X-XSRF-Token";
            });

            // Add DataProtection, but distinguish between dev and production. On localhost keys file should be stored
            // in a shared directory, so that switching tenants does not try to encrypt existing cookies with the wrong key.
            var dataProtectionRoot = CommonHelper.IsDevEnvironment ? appContext.AppDataRoot : appContext.TenantRoot;
            var dataProtectionDir = new DirectoryInfo(Path.Combine(dataProtectionRoot.Root, "DataProtection-Keys"));
            services.AddDataProtection()
                .PersistKeysToFileSystem(dataProtectionDir)
                .AddKeyManagementOptions(o => o.XmlEncryptor ??= new NullXmlEncryptor())
                .SetApplicationName(appContext.AppConfiguration.ApplicationName.NullEmpty() ?? appContext.RuntimeInfo.ApplicationIdentifier);

            // Add default HTTP client
            services.AddHttpClient(string.Empty)
                .AddSmartstoreUserAgent();

            // Add HTTP client for local calls
            services.AddHttpClient("local")
                .AddSmartstoreUserAgent()
                .SkipCertificateValidation()
                .PropagateCookies();

            // Add session feature
            services.AddSession(o =>
            {
                o.Cookie.Name = CookieNames.Session;
                o.Cookie.IsEssential = true;
            });

            // Detailed database related error notifications
            services.AddDatabaseDeveloperPageExceptionFilter();

            services.Configure<WebEncoderOptions>(o =>
            {
                o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All);
            });

            // Add response compression
            services.AddResponseCompression(o =>
            {
                o.EnableForHttps = true;
                o.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { "image/svg+xml" });
            });
        }

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            builder.RegisterType<DefaultViewInvoker>().As<IViewInvoker>().InstancePerLifetimeScope();
            builder.RegisterType<SlugRouteTransformer>().InstancePerLifetimeScope();
        }

        public override void BuildPipeline(RequestPipelineBuilder builder)
        {
            var appContext = builder.ApplicationContext;

            builder.Configure(StarterOrdering.BeforeExceptionHandlerMiddleware, app =>
            {
                // Must come very early.
                app.UseContextState();
            });

            builder.Configure(StarterOrdering.ExceptionHandlerMiddleware, app =>
            {
                bool useDevExceptionPage = appContext.AppConfiguration.UseDeveloperExceptionPage ?? appContext.HostEnvironment.IsDevelopment();
                if (useDevExceptionPage)
                {
                    app.UseDeveloperExceptionPage();
                }
                else
                {
                    app.UseExceptionHandler("/Error");
                    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                    app.UseHsts();
                }

                app.UseStatusCodePagesWithReExecute("/Error/{0}");
            });

            builder.Configure(StarterOrdering.BeforeAuthenticationMiddleware, app =>
            {
                app.UseCookiePolicy();
            });

            builder.Configure(StarterOrdering.AuthenticationMiddleware, app =>
            {
                if (appContext.IsInstalled)
                {
                    app.UseAuthentication();

                    // Initialize work context right after authentication
                    app.UseWorkContext();
                }
            });

            builder.Configure(StarterOrdering.AfterAuthenticationMiddleware, app =>
            {
                if (appContext.IsInstalled)
                {
                    // Write streamlined request completion events, instead of the more verbose ones from the framework.
                    // To use the default framework request logging instead, remove this line and set the "Microsoft"
                    // level in appsettings.json to "Information".
                    app.UseRequestLogging();
                }
            });

            builder.Configure(StarterOrdering.BeforeStaticFilesMiddleware - 5, app =>
            {
                if (appContext.Services.Resolve<PerformanceSettings>().UseResponseCompression)
                {
                    app.UseResponseCompression();
                } 
            });

            builder.Configure(StarterOrdering.AfterStaticFilesMiddleware, app =>
            {
                if (appContext.IsInstalled)
                {
                    // Executes IApplicationInitializer implementations during the very first request.
                    app.UseApplicationInitializer();
                }
            });

            builder.Configure(StarterOrdering.RoutingMiddleware, app =>
            {
                app.UseRouting();
            });

            builder.Configure(StarterOrdering.AfterRoutingMiddleware, app =>
            {
                if (appContext.IsInstalled)
                {
                    app.UseAuthorization();
                }
            });

            builder.Configure(StarterOrdering.EarlyMiddleware, app =>
            {
                app.UseSession();
                app.UseCheckoutState();

                if (appContext.IsInstalled)
                {
                    app.UseUrlPolicy();
                    app.UseRequestCulture();
                }
            });
        }

        public override void MapRoutes(EndpointRoutingBuilder builder)
        {
            if (!builder.ApplicationContext.IsInstalled)
            {
                return;
            }

            builder.MapRoutes(StarterOrdering.EarlyRoute, routes =>
            {
                routes.MapDynamicControllerRoute<SlugRouteTransformer>("{**slug:minlength(2)}");
            });
        }
    }

    internal class LastRoutes : StarterBase
    {
        public override bool Matches(IApplicationContext appContext)
            => appContext.IsInstalled;

        public override void MapRoutes(EndpointRoutingBuilder builder)
        {
            builder.MapRoutes(StarterOrdering.LastRoute, routes =>
            {
                // Register routes from SlugRouteTransformer solely needed for URL creation, NOT for route matching.
                SlugRouteTransformer.Routers.Each(x => x.MapRoutes(routes));

                // TODO: (core) Very last route: PageNotFound?
                routes.MapControllerRoute("PageNotFound", "{*path}", new { controller = "Error", action = "NotFound" });
            });
        }
    }
}