using Smartstore.Http;

namespace Smartstore.Engine.Modularity
{
    /// <summary>
    /// Marker interface for a provider
    /// </summary>
    public interface IProvider
    {
    }

    /// <summary>
    /// Marker interface for a user editable provider
    /// </summary>
    public interface IUserEditable
    {
    }

    /// <summary>
    /// Marks a concrete provider or module implementation as configurable via backend
    /// </summary>
    public interface IConfigurable
    {
        /// <summary>
        /// Gets a route for provider or module configuration
        /// </summary>
        RouteInfo GetConfigurationRoute();
    }
}
