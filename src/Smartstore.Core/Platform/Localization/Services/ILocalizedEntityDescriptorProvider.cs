namespace Smartstore.Core.Localization
{
    /// <summary>
    /// Responsible for determining localized entity metadata for all active entity types.
    /// </summary>
    public interface ILocalizedEntityDescriptorProvider
    {
        /// <summary>
        /// Gets a list of all localized entities that implement <see cref="ILocalizedEntity"/>
        /// and define the <see cref="LocalizedEntityAttribute"/> attribute.
        /// </summary>
        IReadOnlyList<LocalizedEntityDescriptor> GetEntityDescriptors();
    }
}
