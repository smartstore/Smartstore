namespace Smartstore.Domain
{
    public interface INamedEntity
    {
        /// <summary>
        /// Gets the entities conceptual name.
        /// </summary>
        string GetEntityName();
    }

    public interface IDisplayedEntity : INamedEntity
    {
        /// <summary>
        /// Gets the entity member/property names for the display name value.
        /// </summary>
        string[] GetDisplayNameMemberNames();

        /// <summary>
        /// Gets the entity display name to convert to a seo friendly URL slug.
        /// </summary>
        string GetDisplayName();
    }
}
