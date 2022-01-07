using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Identity
{
    public static class ExternalAuthenticationExtensions
    {
        public static bool IsMethodActive(this Provider<IExternalAuthenticationMethod> method, ExternalAuthenticationSettings settings)
        {
            Guard.NotNull(method, nameof(method));

            return method.ToLazy().IsMethodActive(settings);
        }

        public static bool IsMethodActive(this Lazy<IExternalAuthenticationMethod, ProviderMetadata> method, ExternalAuthenticationSettings settings)
        {
            Guard.NotNull(method, nameof(method));
            Guard.NotNull(settings, nameof(settings));

            if (settings.ActiveAuthenticationMethodSystemNames == null)
            {
                return false;
            }

            return settings.ActiveAuthenticationMethodSystemNames.Contains(method.Metadata.SystemName, StringComparer.OrdinalIgnoreCase);
        }
    }
}
