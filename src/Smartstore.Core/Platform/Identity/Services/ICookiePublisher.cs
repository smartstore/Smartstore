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
        Task<IEnumerable<CookieInfo>> GetCookieInfosAsync();
    }

    /// <summary>
    /// Module cookie infos.
    /// </summary>
    public class CookieInfo : ILocalizedEntity, IDisplayedEntity
    {
        int ILocalizedEntity.Id
            => Name.IsEmpty() ? 0 : Name.GetHashCode();

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
        /// <summary>
        /// Specifies that required cookies can be set.
        /// </summary>
        Required,

        /// <summary>
        /// Specifies that analytical cookies can be set.
        /// </summary>
        Analytics,

        /// <summary>
        /// Specifies that third party cookies can be set.
        /// </summary>
        ThirdParty,

        /// <summary>
        /// Specifies that ad user data can be sent to third parties.
        /// </summary>
        ConsentAdUserData,

        /// <summary>
        /// Specifies that ad personalization is desired by the user.
        /// </summary>
        ConsentAdPersonalization
    }
}
