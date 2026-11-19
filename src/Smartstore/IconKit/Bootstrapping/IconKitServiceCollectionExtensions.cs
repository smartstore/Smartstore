using Smartstore.IconKit.Cache;
using Smartstore.IconKit.Sources;
using Smartstore.IconKit.Sprites;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using Smartstore.IconKit;

namespace Smartstore.Bootstrapping;

/// <summary>
/// Registers icon services and their configurable source and cache locations.
/// </summary>
public static class IconKitServiceCollectionExtensions
{
    /// <summary>
    /// Adds IconKit services. Repeated calls can configure options without duplicating services.
    /// </summary>
    /// <param name="services">The application's service collection.</param>
    /// <param name="configure">Optional configuration for icon folders and routes.</param>
    public static IServiceCollection AddIconKit(this IServiceCollection services, Action<IconKitOptions> configure = null)
    {
        Guard.NotNull(services);

        services.AddOptions<IconKitOptions>();
        if (configure != null)
        {
            services.Configure(configure);
        }

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<IconKitOptions>, ConfigureIconKitOptions>());
        services.AddHttpContextAccessor();
        services.TryAddSingleton<IIconCache, IconCache>();
        services.TryAddSingleton<IIconCacheLifecycle, IconCacheLifecycle>();
        services.TryAddSingleton<IIconCacheRecovery, IconCacheRecovery>();
        services.TryAddSingleton<IIconSpritePlanner, IconSpritePlanner>();
        services.TryAddSingleton<IIconAreaResolver, IconAreaResolver>();
        services.TryAddSingleton<IIconCatalogAccessor, IconCatalogAccessor>();
        services.TryAddSingleton<IIconService, IconService>();
        services.TryAddSingleton<IIconKitService, IconKitService>();
        services.TryAddSingleton<IIconBrowser, IconBrowser>();
        services.TryAddScoped<IIconRenderer, IconRenderer>();
        return services;
    }

    private sealed class ConfigureIconKitOptions : IPostConfigureOptions<IconKitOptions>
    {
        public void PostConfigure(string name, IconKitOptions options)
        {
            Guard.NotEmpty(options.SourcePath);
            Guard.NotEmpty(options.CachePath);
            Guard.NotEmpty(options.RoutePrefix);

            options.RoutePrefix = "/" + options.RoutePrefix.Trim('/');
            if (options.RoutePrefix == "/" || options.RoutePrefix.Any(c => char.IsWhiteSpace(c) || "?#{ }\\".Contains(c)))
            {
                throw new ArgumentException("The IconKit route prefix must be a literal application-relative path.", nameof(options));
            }
        }
    }
}
