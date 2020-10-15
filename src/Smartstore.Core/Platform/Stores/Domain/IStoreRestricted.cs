namespace Smartstore.Core.Stores
{
    /// <summary>
    /// Represents an entity which supports store mapping
    /// </summary>
    public partial interface IStoreRestricted
    {
        /// <summary>
        /// Gets or sets a value indicating whether the entity is limited/restricted to certain stores
        /// </summary>
        bool LimitedToStores { get; set; }
    }
}