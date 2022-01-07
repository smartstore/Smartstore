using Smartstore.Core.Localization;

namespace Smartstore.Core.Identity
{
    /// <summary>
    /// Marks a module as a cookie publisher. 
    /// </summary>
    public interface ICookiePublisher
    {
        /// <summary>
        /// Gets the cookie info of the cookie publisher (e.g. a module).
        /// </summary>
        Task<IEnumerable<CookieInfo>> GetCookieInfoAsync();
    }

    /// <summary>
    /// Module cookie infos.
    /// </summary>
    public class CookieInfo : ILocalizedEntity
    {
        int ILocalizedEntity.Id => 0;
        string INamedEntity.GetEntityName() => Name;

        /// <summary>
        /// Name of the module.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the cookie (e.g. purpose of using the cookie).
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Selected store identifiers.
        /// </summary>
        public int[] SelectedStoreIds { get; set; }

        /// <summary>
        /// Type of the cookie.
        /// </summary>
        public CookieType CookieType { get; set; }
    }

    /// <summary>
    /// Type of the cookie.
    /// </summary>
    public enum CookieType
    {
        Required,
        Analytics,
        ThirdParty
    }
}
