using Smartstore.Core.Localization;
using Smartstore.Utilities;

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
        Task<IEnumerable<CookieInfo>> GetCookieInfosAsync();
    }

    /// <summary>
    /// Module cookie infos.
    /// </summary>
    public class CookieInfo : ILocalizedEntity, IDisplayedEntity
    {
        int ILocalizedEntity.Id
            => Name.IsEmpty() ? 0 : (int)XxHashUnsafe.ComputeHash(Name);

        string INamedEntity.GetEntityName() 
            => nameof(CookieInfo);

        string[] IDisplayedEntity.GetDisplayNameMemberNames()
            => new[] { nameof(Name) };

        string IDisplayedEntity.GetDisplayName()
            => Name;

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
