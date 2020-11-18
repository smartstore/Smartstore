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
        /// Gets the entity member/property name for display name value.
        /// </summary>
        string GetDisplayNameMemberName();

        /// <summary>
        /// Gets the entity display name to convert to a seo friendly URL slug.
        /// </summary>
        string GetDisplayName();
    }
}
