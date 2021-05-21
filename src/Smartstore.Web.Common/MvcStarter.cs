using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Autofac;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders;
using Newtonsoft.Json;
using Smartstore.ComponentModel;
using Smartstore.Core.Bootstrapping;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Logging.Serilog;
using Smartstore.Core.Web;
using Smartstore.Data;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.IO;
using Smartstore.Net;
using Smartstore.Web.Bootstrapping;
using Smartstore.Web.Modelling;
using Smartstore.Web.Modelling.Settings;
using Smartstore.Web.Modelling.Validation;
using Smartstore.Web.Razor;

namespace Smartstore.Web
{
    internal class MvcStarter : StarterBase
    {
        public MvcStarter()
        {
            RunAfter<WebStarter>();
        }
        
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext, bool isActiveModule)
        {
            // Add action context accessor
            services.AddTransient<IActionContextAccessor, ActionContextAccessor>();

            if (appContext.IsInstalled)
            {
                // Configure Cookie Policy Options
                services.AddSingleton<IConfigureOptions<CookiePolicyOptions>, CookiePolicyOptionsConfigurer>();

                services.Configure<RazorViewEngineOptions>(o =>
                {
                    o.ViewLocationExpanders.Add(new ThemeViewLocationExpander());
                    o.ViewLocationExpanders.Add(new AdminViewLocationExpander());
                    o.ViewLocationExpanders.Add(new PartialViewLocationExpander());

                    if (appContext.AppConfiguration.EnableLocalizedViews)
                    {
                        o.ViewLocationExpanders.Add(new LanguageViewLocationExpander(LanguageViewLocationExpanderFormat.Suffix));
                    }

                    // TODO: (core) Implement ModuleViewLocationExpander
                });
            }

            // Add AntiForgery
            services.AddAntiforgery(o => 
            {
                o.Cookie.Name = CookieNames.Antiforgery;
                o.HeaderName = "X-XSRF-Token";
            });

            // Add HTTP client feature
            services.AddHttpClient();
            
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

            // TODO: (core) Implement localization stuff
            //services.TryAddSingleton<IStringLocalizerFactory, SmartStringLocalizerFactory>();
            //services.TryAddScoped(typeof(IStringLocalizer<>), typeof(SmartStringLocalizer<>));

            services.AddRouting(o => 
            {
                // TODO: (core) Make this behave like in SMNET
                o.AppendTrailingSlash = true;
                o.LowercaseUrls = true;
            });

            var mvcBuilder = services
                .AddControllersWithViews(o =>
                {
                    //o.EnableEndpointRouting = false;
                    // TODO: (core) AddModelBindingMessagesLocalizer
                    // TODO: (core) Add model binders
                    o.Filters.AddService<IViewDataAccessor>(int.MinValue);
                })
                .AddRazorRuntimeCompilation(o =>
                {
                    // TODO: (core) FileProvider
                })
                .AddFluentValidation(c =>
                {
                    c.LocalizationEnabled = true;
                    c.ImplicitlyValidateChildProperties = true;

                    // Scan active assemblies for validators
                    c.RegisterValidatorsFromAssemblies(appContext.TypeScanner.Assemblies, lifetime: ServiceLifetime.Scoped);

                    var opts = c.ValidatorOptions;

                    // It sais 'not recommended', but who cares: SAVE RAM!
                    opts.DisableAccessorCache = true;

                    // Language Manager
                    opts.LanguageManager = new ValidatorLanguageManager(appContext);

                    // Display name resolver
                    var originalDisplayNameResolver = opts.DisplayNameResolver;
                    opts.DisplayNameResolver = (type, member, expression) =>
                    {
                        string name = null;

                        if (expression != null && member != null)
                        {
                            var metadataProvider = EngineContext.Current.Application.Services.Resolve<IModelMetadataProvider>();
                            var metadata = metadataProvider.GetMetadataForProperty(type, member.Name);
                            name = metadata.DisplayName;
                        }

                        return name ?? originalDisplayNameResolver.Invoke(type, member, expression);
                    };
                })
                .AddNewtonsoftJson(o =>
                {
                    var settings = o.SerializerSettings;
                    settings.ContractResolver = SmartContractResolver.Instance;
                    settings.TypeNameHandling = TypeNameHandling.Objects;
                    settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                    settings.NullValueHandling = NullValueHandling.Ignore;
                    settings.MaxDepth = 32;
                })
                .AddControllersAsServices()
                .AddAppLocalization()
                .AddMvcOptions(o =>
                {
                    // TODO: (core) More MVC config?
                    if (DataSettings.DatabaseIsInstalled())
                    {
                        // Register custom metadata provider
                        o.ModelMetadataDetailsProviders.Add(new SmartDisplayMetadataProvider());
                        o.ModelMetadataDetailsProviders.Add(new AdditionalMetadataProvider());
                    }
                });

            // Add TempData feature
            if (appContext.AppConfiguration.UseCookieTempDataProvider)
            {
                mvcBuilder.AddCookieTempDataProvider(o =>
                {
                    o.Cookie.Name = CookieNames.TempData;
                    o.Cookie.IsEssential = true;
                });
            }
            else
            {
                mvcBuilder.AddSessionStateTempDataProvider();
            }

            // Replace BsonTempDataSerializer that was registered by AddNewtonsoftJson()
            // with our own serializer which is capable of serializing more stuff.
            services.AddSingleton<TempDataSerializer, SmartTempDataSerializer>();
        }

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            builder.RegisterType<DefaultViewDataAccessor>().As<IViewDataAccessor>().InstancePerLifetimeScope();
            builder.RegisterType<StoreDependingSettingHelper>().AsSelf().InstancePerLifetimeScope();

            // Convenience: Register IUrlHelper as transient dependency.
            builder.Register<IUrlHelper>(c =>
            {
                var httpContext = c.Resolve<IHttpContextAccessor>().HttpContext;
                if (httpContext?.Items != null && httpContext.Items.TryGetValue(typeof(IUrlHelper), out var value) && value is IUrlHelper)
                {
                    // We know for sure that IUrlHelper is saved in HttpContext.Items
                    return (IUrlHelper)value;
                }

                var actionContext = c.Resolve<IActionContextAccessor>().ActionContext;
                if (actionContext != null)
                {
                    return c.Resolve<IUrlHelperFactory>().GetUrlHelper(actionContext);
                }

                return null;
            }).InstancePerDependency();

            if (DataSettings.DatabaseIsInstalled())
            {
                builder.RegisterDecorator<SmartLinkGenerator, LinkGenerator>();
                builder.RegisterDecorator<SmartRouteValuesAddressScheme, IEndpointAddressScheme<RouteValuesAddress>>();
            }
        }

        public override void BuildPipeline(RequestPipelineBuilder builder)
        {
            var appContext = builder.ApplicationContext;

            builder.Configure(StarterOrdering.BeforeStaticFilesMiddleware, app => 
            {
                if (appContext.HostEnvironment.IsDevelopment() || appContext.AppConfiguration.UseDeveloperExceptionPage)
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

            builder.Configure(StarterOrdering.StaticFilesMiddleware, app =>
            {
                // TODO: (core) Set StaticFileOptions
                app.UseStaticFiles(new StaticFileOptions 
                { 
                    FileProvider = appContext.WebRoot,
                    ContentTypeProvider = MimeTypes.ContentTypeProvider
                }); 
            });

            builder.Configure(StarterOrdering.FirstMiddleware, app =>
            {
                // TODO: (core) Find decent ordering for MiniProfiler
                app.UseMiniProfiler();
            });

            builder.Configure(StarterOrdering.RoutingMiddleware, app =>
            {
                app.UseRouting();
            });

            builder.Configure(StarterOrdering.AfterRoutingMiddleware, app =>
            {
                // TODO: (core) Use Swagger
                // TODO: (core) Use Response compression
            });

            builder.Configure(StarterOrdering.EarlyMiddleware, app =>
            {
                // TODO: (core) Configure session
                app.UseSession();

                if (appContext.IsInstalled)
                {
                    app.UseUrlPolicy();
                    app.UseRequestCulture();
                    app.UseMiddleware<SerilogHttpContextMiddleware>();
                }
            });

            builder.Configure(StarterOrdering.DefaultMiddleware, app =>
            {
                // TODO: (core) Configure cookie policy
                app.UseCookiePolicy();
            });
        }

        public override void MapRoutes(EndpointRoutingBuilder builder)
        {
            builder.MapRoutes(StarterOrdering.EarlyRoute, routes =>
            {
                routes.MapControllerRoute(
                    name: "areas",
                    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

                routes.MapXmlSitemap();

                //routes.MapControllers();

                routes.MapLocalizedControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}