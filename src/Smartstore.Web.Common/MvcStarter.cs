using Autofac;
using FluentValidation;
using FluentValidation.AspNetCore;
using FluentValidation.Resources;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Smartstore.ComponentModel;
using Smartstore.Core.Bootstrapping;
using Smartstore.Core.Common.JsonConverters;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Seo;
using Smartstore.Core.Web;
using Smartstore.Engine.Builders;
using Smartstore.Engine.Modularity;
using Smartstore.Engine.Modularity.ApplicationParts;
using Smartstore.Net;
using Smartstore.Web.Controllers;
using Smartstore.Web.Filters;
using Smartstore.Web.Modelling;
using Smartstore.Web.Modelling.Settings;
using Smartstore.Web.Modelling.Validation;
using Smartstore.Web.Models.DataGrid;
using Smartstore.Web.Razor;
using Smartstore.Web.Rendering;
using Smartstore.Web.Routing;

namespace Smartstore.Web
{
    internal class MvcStarter : StarterBase
    {
        public MvcStarter()
        {
            RunAfter<WebStarter>();
        }

        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            // Add action context accessor
            services.AddTransient<IActionContextAccessor, ActionContextAccessor>();

            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IFilterProvider, ConditionalFilterProvider>());

            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IConfigureOptions<RouteOptions>, RouteOptionsConfigurer>());

            services.AddRouting();

            // Replace BsonTempDataSerializer that was registered by AddNewtonsoftJson()
            // with our own serializer which is capable of serializing more stuff.
            services.AddSingleton<TempDataSerializer, SmartTempDataSerializer>();

            // Replaces inbuilt IHtmlGenerator with SmartHtmlGenerator
            // that is capable of applying custom Bootstrap classes to generated html.
            services.AddSingleton<IHtmlGenerator, SmartHtmlGenerator>();

            // ActionResult executor for LazyFileContentResult
            services.AddSingleton<IActionResultExecutor<LazyFileContentResult>, LazyFileContentResultExecutor>();
        }

        public override void ConfigureMvc(IMvcBuilder mvcBuilder, IServiceCollection services, IApplicationContext appContext)
        {
            // Populate application parts with modules
            mvcBuilder.PartManager.PopulateModules(appContext);

            var validatorLanguageManager = new ValidatorLanguageManager(appContext);

            mvcBuilder
                .AddMvcOptions(o =>
                {
                    o.Filters.Add<ModulePopulatorFilter>(int.MinValue);
                    o.Filters.AddService<IViewDataAccessor>(int.MinValue);
                    
                    o.ModelBinderProviders.Insert(0, new InvariantFloatingPointTypeModelBinderProvider());

                    // Register custom metadata provider
                    o.ModelMetadataDetailsProviders.Add(new AdditionalMetadataProvider());

                    if (appContext.IsInstalled)
                    {
                        o.ModelMetadataDetailsProviders.Add(new SmartDisplayMetadataProvider());

                        // Localized messages
                        o.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(x =>
                        {
                            return validatorLanguageManager.GetErrorMessage("MustBeANumber", x);
                        });
                        o.ModelBindingMessageProvider.SetNonPropertyValueMustBeANumberAccessor(() =>
                        {
                            return validatorLanguageManager.GetString("NonPropertyMustBeANumber");
                        });
                    }
                })
                .AddRazorOptions(o =>
                {
                    if (appContext.IsInstalled)
                    {
                        o.ViewLocationExpanders.Add(new ThemeViewLocationExpander());
                        o.ViewLocationExpanders.Add(new ModuleViewLocationExpander(appContext.ModuleCatalog));
                        o.ViewLocationExpanders.Add(new PartialViewLocationExpander());
                    }
                    
                    if (appContext.AppConfiguration.EnableLocalizedViews)
                    {
                        o.ViewLocationExpanders.Add(new LanguageViewLocationExpander(LanguageViewLocationExpanderFormat.Suffix));
                    }
                })
                .AddNewtonsoftJson(o =>
                {
                    var settings = o.SerializerSettings;
                    settings.ContractResolver = SmartContractResolver.Instance;
                    settings.TypeNameHandling = TypeNameHandling.None;
                    settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                    settings.NullValueHandling = NullValueHandling.Ignore;
                    settings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
                    settings.MaxDepth = 32;
                    settings.Converters.Add(new UTCDateTimeConverter(new IsoDateTimeConverter()));
                    settings.Converters.Add(new StringEnumConverter());
                })
                .AddControllersAsServices()
                .AddViewOptions(o =>
                {
                    o.HtmlHelperOptions.CheckBoxHiddenInputRenderMode = CheckBoxHiddenInputRenderMode.Inline;

                    // Client validation (must come last - after "FluentValidationClientModelValidatorProvider")
                    o.ClientModelValidatorProviders.Add(new SmartClientModelValidatorProvider(appContext, validatorLanguageManager));
                });

            // Add and configure FluentValidator
            AddFluentValidator(services, appContext, validatorLanguageManager);

            // Add Razor runtime compilation if enabled
            if (appContext.AppConfiguration.EnableRazorRuntimeCompilation)
            {
                mvcBuilder.AddRazorRuntimeCompilation(o =>
                {
                    o.FileProviders.Clear();
                    o.FileProviders.Add(new RazorRuntimeFileProvider(appContext, false));
                });
            }

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
        }

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            // Register all module entry types in service container
            builder.RegisterModule(new ModularityModule(appContext));

            builder.RegisterType<DefaultViewDataAccessor>().As<IViewDataAccessor>().InstancePerLifetimeScope();
            builder.RegisterType<GridCommandStateStore>().As<IGridCommandStateStore>().InstancePerLifetimeScope();
            builder.RegisterType<MultiStoreSettingHelper>().AsSelf().InstancePerLifetimeScope();

            // Convenience: Register IUrlHelper as transient dependency.
            builder.Register<IUrlHelper>(ResolveUrlHelper).InstancePerDependency();

            if (appContext.IsInstalled)
            {
                builder.RegisterDecorator<SmartLinkGenerator, LinkGenerator>();
                builder.RegisterType<AIToolHtmlGenerator>().AsSelf().InstancePerLifetimeScope();
            }
        }

        public override void MapRoutes(EndpointRoutingBuilder builder)
        {
            builder.MapRoutes(StarterOrdering.DefaultRoute, routes =>
            {
                routes.MapControllerRoute(
                    name: "areas",
                    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

                routes.MapXmlSitemap();

                routes.MapLocalizedControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private static void AddFluentValidator(IServiceCollection services, IApplicationContext appContext, ILanguageManager languageManager)
        {
            services
                .AddFluentValidationAutoValidation()
                .AddFluentValidationClientsideAdapters()
                .AddValidatorsFromAssemblies(appContext.TypeScanner.Assemblies);

            var opts = ValidatorOptions.Global;

            // It sais 'not recommended', but who cares: SAVE RAM!
            opts.DisableAccessorCache = true;

            // Language Manager
            opts.LanguageManager = languageManager;

            // Display name resolver
            var originalDisplayNameResolver = opts.DisplayNameResolver;
            opts.DisplayNameResolver = (type, member, expression) =>
            {
                string name = null;

                if (expression != null && member != null)
                {
                    var metadataProvider = EngineContext.Current.Application.Services.Resolve<IModelMetadataProvider>();
                    var metadata = metadataProvider.GetMetadataForProperty(member.DeclaringType, member.Name);
                    name = metadata.DisplayName;
                }

                return name ?? originalDisplayNameResolver.Invoke(type, member, expression);
            };
        }

        private static IUrlHelper ResolveUrlHelper(IComponentContext c)
        {
            var httpContext = c.Resolve<IHttpContextAccessor>().HttpContext;

            if (httpContext?.Items == null)
            {
                throw new InvalidOperationException($"Cannot resolve '{nameof(IUrlHelper)}' because '{nameof(HttpContext)}.{nameof(HttpContext.Items)}' was null. Pass '{typeof(Lazy<IUrlHelper>).Name}' or '{typeof(IUrlHelperFactory).Name}' to the constructor instead.");
            }

            if (httpContext.Items.TryGetValue(typeof(IUrlHelper), out var value) && value is IUrlHelper urlHelper)
            {
                // We know for sure that IUrlHelper is saved in HttpContext.Items
                return urlHelper;
            }

            var actionContext = c.Resolve<IActionContextAccessor>().ActionContext;
            if (actionContext != null)
            {
                // ActionContext is available (also Endpoint). Resolve EndpointRoutingUrlHelper.
                return c.Resolve<IUrlHelperFactory>().GetUrlHelper(actionContext);
            }

            // No ActionContext. Create an IUrlHelper that can work outside of routing endpoints (e.g. in middlewares)
            var routeData = httpContext.GetRouteData();
            if (routeData == null)
            {
                routeData = new RouteData();
            }

            urlHelper = new SmartUrlHelper(
                new ActionContext(httpContext, routeData, new ActionDescriptor()),
                c.Resolve<LinkGenerator>());

            // Better not to interfere with UrlHelperFactory, so don't save in Items.
            // httpContext.Items[typeof(IUrlHelper)] = urlHelper;

            return urlHelper;
        }

        internal sealed class RouteOptionsConfigurer : IConfigureOptions<RouteOptions>
        {
            private readonly IApplicationContext _appContext;

            public RouteOptionsConfigurer(IApplicationContext appContext)
            {
                _appContext = appContext;
            }

            public void Configure(RouteOptions options)
            {
                if (_appContext.IsInstalled)
                {
                    var seoSettings = _appContext.Services.Resolve<SeoSettings>();
                    options.AppendTrailingSlash = seoSettings.AppendTrailingSlashToUrls;
                    options.LowercaseUrls = true; // seoSettings.LowercaseUrls;
                    options.LowercaseQueryStrings = false; // seoSettings.LowercaseQueryStrings;
                }
                else
                {
                    options.AppendTrailingSlash = true;
                    options.LowercaseUrls = true;
                }
            }
        }
    }
}