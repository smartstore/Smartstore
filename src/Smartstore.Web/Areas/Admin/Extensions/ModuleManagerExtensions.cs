using System.Collections.Concurrent;
using Smartstore.Admin.Models.Modularity;
using Smartstore.Engine.Modularity;
using Smartstore.Http;

namespace Smartstore.Admin
{
    public static class ModuleManagerExtensions
    {
        private static readonly ConcurrentDictionary<string, RouteInfo> _routesCache = new();

        public static ProviderModel ToProviderModel(
            this ModuleManager manager,
            Provider<IProvider> provider,
            bool forEdit = false,
            Action<Provider<IProvider>, ProviderModel> setup = null)
        {
            return ToProviderModel<IProvider, ProviderModel>(manager, provider, forEdit, setup);
        }

        public static TModel ToProviderModel<TProvider, TModel>(
            this ModuleManager manager,
            Provider<TProvider> provider,
            bool forEdit = false,
            Action<Provider<TProvider>, TModel> setup = null)
            where TModel : ProviderModel, new()
            where TProvider : IProvider
        {
            Guard.NotNull(provider, nameof(provider));

            var metadata = provider.Metadata;
            var model = ToProviderModel<TModel>(manager, metadata, forEdit);

            if (metadata.IsConfigurable)
            {
                var routeInfo = _routesCache.GetOrAdd(model.SystemName, (key) =>
                {
                    var configurable = (IConfigurable)provider.Value;
                    var route = configurable.GetConfigurationRoute();

                    if (route.Action.IsEmpty())
                    {
                        metadata.IsConfigurable = false;
                        return null;
                    }
                    else
                    {
                        return route;
                    }
                });

                if (routeInfo != null)
                {
                    model.ConfigurationRoute = new RouteInfo(routeInfo);
                }
            }

            setup?.Invoke(provider, model);

            model.IsConfigurable = metadata.IsConfigurable;

            return model;
        }

        public static ProviderModel ToProviderModel(this ModuleManager manager, ProviderMetadata metadata, bool forEdit = false)
            => ToProviderModel<ProviderModel>(manager, metadata, forEdit);

        public static TModel ToProviderModel<TModel>(this ModuleManager manager, ProviderMetadata metadata, bool forEdit = false)
            where TModel : ProviderModel, new()
        {
            Guard.NotNull(metadata, nameof(metadata));

            var model = new TModel
            {
                ProviderType = metadata.ProviderType,
                SystemName = metadata.SystemName,
                FriendlyName = forEdit ? metadata.FriendlyName : manager.GetLocalizedFriendlyName(metadata),
                Description = forEdit ? metadata.Description : manager.GetLocalizedDescription(metadata),
                DisplayOrder = metadata.DisplayOrder,
                IsEditable = metadata.IsEditable,
                IconUrl = manager.GetIconUrl(metadata),
                ModuleDescriptor = metadata.ModuleDescriptor,
                IsConfigurable = metadata.IsConfigurable
            };

            if (model.ModuleDescriptor != null)
            {
                model.ProvidingModuleFriendlyName = manager.GetLocalizedFriendlyName(model.ModuleDescriptor);
            }

            return model;
        }
    }
}
