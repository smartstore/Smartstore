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
    /// Marks a concrete provider or module implementation as configurable through the backend
    /// </summary>
    public interface IConfigurable
    {
        //// TODO: (core) IConfigurable > Implement routing.
        ///
        ///// <summary>
        ///// Gets a route for provider or plugin configuration
        ///// </summary>
        ///// <param name="actionName">Action name</param>
        ///// <param name="controllerName">Controller name</param>
        ///// <param name="routeValues">Route values</param>
        //void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues);
    }
}
