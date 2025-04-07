namespace Smartstore.Core.Stores
{
    /// <summary>
    /// Represents an entity whose existence in the frontend is limited to certain store(s).
    /// </summary>
    public partial interface IStoreRestricted
    {
        /// <summary>
        /// Gets or sets a value indicating whether the entity is limited/restricted to certain store(s).
        /// </summary>
        bool LimitedToStores { get; set; }
    }
}