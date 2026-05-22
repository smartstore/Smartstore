namespace Smartstore.Core.Stores;

/// <summary>
/// Defines a contract for objects that are associated with a specific store scope.
/// </summary>
/// <remarks>Implement this interface to indicate that an object is relevant to a particular store context,
/// typically in multi-store configuration. The store scope can be used to filter or segregate data and behavior based on
/// the current store.</remarks>
public interface IStoreScoped
{
    /// <summary>
    /// Gets the identifier of the store scope associated with the current context.
    /// </summary>
    /// <remarks>Use this property to determine which store's settings or data are currently in effect. A
    /// value of 0 typically indicates the global or default scope, while other values correspond to specific store
    /// identifiers.</remarks>
    int StoreScope { get; }
}