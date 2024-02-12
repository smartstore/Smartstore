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
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders;
using Smartstore.Bootstrapping;
using Smartstore.Core.Bootstrapping;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Seo.Routing;
using Smartstore.Engine.Builders;
using Smartstore.Net;
using Smartstore.Net.Http;
using Smartstore.Utilities;
using Smartstore.Web.Bootstrapping;

namespace Smartstore.Web
{
    internal class WebStarter : StarterBase
    {
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            services.AddWorkContext(appContext);

            if (appContext.IsInstalled)
            {
                // Configure Cookie Policy Options
                services.TryAddEnumerable(
                    ServiceDescriptor.Singleton<IConfigureOptions<CookiePolicyOptions>, CookiePolicyOptionsConfigurer>());
            }

            // Add AntiForgery
            services.AddAntiforgery(o =>
            {
                o.Cookie.Name = CookieNames.Antiforgery;
                o.HeaderName = "X-XSRF-Token";
            });

            // HSTS
            services.AddHsts(options =>
            {
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(appContext.AppConfiguration.HstsMaxAge);
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
                o.IdleTimeout = TimeSpan.FromMinutes(30);
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
            builder.RegisterType<SlugRouteTransformer>().InstancePerLifetimeScope();
        }

        public override void BuildPipeline(RequestPipelineBuilder builder)
        {
            var appContext = builder.ApplicationContext;

            builder.Configure(StarterOrdering.BeforeExceptionHandlerMiddleware, app =>
            {
                // Reverse Proxy
                var proxy = appContext.AppConfiguration.ReverseProxy;
                if (proxy != null && proxy.Enabled)
                {
                    // Map the ForwaredHeadersMiddleware
                    app.UseForwardedHeaders(MapForwardedHeadersOptions(proxy));

                    if (proxy.ForwardPrefixHeader)
                    {
                        // The ForwardedHeadersMiddleware evaluates the X-Forwarded-Prefix correctly
                        // by setting PathBase, but does not necessarily substitute the base path
                        // from the current request path (which seems to be by design). In order for routing to
                        // work correctly we have to deal with it programmatically.
                        app.Use((context, next) => 
                        {
                            var req = context.Request;
                            if (req.PathBase.HasValue && req.Path.StartsWithSegments(req.PathBase, out var remainingPath))
                            {
                                req.Path = remainingPath;
                            }

                            return next(context);
                        });
                    }
                }
                
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

            builder.Configure(StarterOrdering.AfterExceptionHandlerMiddleware, app =>
            {
                if (appContext.IsInstalled)
                {
                    // Executes IApplicationInitializer implementations during the very first request.
                    app.UseApplicationInitializer();
                }
            });

            builder.Configure(StarterOrdering.AfterStaticFilesMiddleware, app =>
            {
                app.UsePoweredBy();
                app.UseSecurityHeaders();
            });

            builder.Configure(StarterOrdering.RoutingMiddleware, app =>
            {
                app.UseLocalizedRouting(appContext);
            });

            builder.Configure(StarterOrdering.AfterRewriteMiddleware, app =>
            {
                if (appContext.Services.Resolve<PerformanceSettings>().UseResponseCompression)
                {
                    app.UseResponseCompression();
                }
            });

            builder.Configure(StarterOrdering.BeforeAuthMiddleware, app =>
            {
                app.UseCookiePolicy();
            });

            builder.Configure(StarterOrdering.WorkContextMiddleware, app =>
            {
                if (appContext.IsInstalled)
                {
                    // Initializes work context data
                    app.UseWorkContext();

                    // Write streamlined request completion events, instead of the more verbose ones from the framework.
                    // To use the default framework request logging instead, remove this line and set the "Microsoft"
                    // level in appsettings.json to "Information".
                    app.UseRequestLogging();
                }
            });

            builder.Configure(StarterOrdering.EarlyMiddleware, app =>
            {
                app.UseSession();
                app.UseCheckoutState();
            });
        }

        public override void MapRoutes(EndpointRoutingBuilder builder)
        {
            if (!builder.ApplicationContext.IsInstalled)
            {
                return;
            }

            builder.MapRoutes(StarterOrdering.LateRoute, routes =>
            {
                // Should come late. But in fact, has no effect :-(
                routes.MapDynamicControllerRoute<SlugRouteTransformer>("{**slug:minlength(2)}");
            });

            builder.MapRoutes(StarterOrdering.LastRoute, routes =>
            {
                // Register routes from SlugRouteTransformer solely needed for URL creation, NOT for route matching.
                routes.MapComposite(SlugRouteTransformer.Routers.Select(x => x.MapRoutes(routes)).ToArray())
                    .WithMetadata(new SuppressMatchingMetadata());
            });
        }

        private static ForwardedHeadersOptions MapForwardedHeadersOptions(SmartConfiguration.ProxyConfiguration config)
        {
            var forwardedHeaders = ForwardedHeaders.None;

            if (config.ForwardForHeader)
                forwardedHeaders |= ForwardedHeaders.XForwardedFor;

            if (config.ForwardHostHeader)
                forwardedHeaders |= ForwardedHeaders.XForwardedHost;

            if (config.ForwardProtoHeader)
                forwardedHeaders |= ForwardedHeaders.XForwardedProto;

            if (config.ForwardPrefixHeader)
                forwardedHeaders |= ForwardedHeaders.XForwardedPrefix;

            var options = new ForwardedHeadersOptions
            {
                ForwardedHeaders = forwardedHeaders,
                // IIS already serves as a reverse proxy and will add X-Forwarded headers to all requests,
                // so we need to increase this limit, otherwise, passed forwarding headers will be ignored.
                ForwardLimit = 2
            };

            if (config.ForwardedForHeaderName.HasValue())
                options.ForwardedForHeaderName = config.ForwardedForHeaderName;

            if (config.ForwardedHostHeaderName.HasValue())
                options.ForwardedHostHeaderName = config.ForwardedHostHeaderName;

            if (config.ForwardedProtoHeaderName.HasValue())
                options.ForwardedProtoHeaderName = config.ForwardedProtoHeaderName;

            if (config.ForwardedPrefixHeaderName.HasValue())
                options.ForwardedPrefixHeaderName = config.ForwardedPrefixHeaderName;

            if (config.KnownProxies != null)
            {
                var addresses = config.KnownProxies
                    .Select(x =>
                    {
                        if (IPAddress.TryParse(x, out var ip))
                        {
                            return ip;
                        }

                        return null;
                    })
                    .Where(x => x != null)
                    .ToArray();
                
                options.KnownProxies.AddRange(addresses);
                if (addresses.Length > 0)
                {
                    // Disable limit, because at least one KnownProxy is configured.
                    options.ForwardLimit = null;
                }
            }

            if (config.AllowedHosts != null)
            {
                options.AllowedHosts.AddRange(config.AllowedHosts);
            }

            return options;
        }
    }
}