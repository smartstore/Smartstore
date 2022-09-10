using Smartstore.Data;

namespace Smartstore.Core.Localization
{
    /// <summary>
    /// Responsible for loading the default values of localizable properties from the database.
    /// </summary>
    public interface ILocalizedEntityLoader
    {
        /// <summary>
        /// Determines the count of all entities for the given <paramref name="descriptor"/>.
        /// Also applies <see cref="LocalizedEntityDescriptor.FilterPredicate"/> if set.
        /// </summary>
        int GetCount(LocalizedEntityDescriptor descriptor);

        /// <summary>
        /// Loads dynamically shaped entities for given <paramref name="descriptor"/>.
        /// The dynamic instances only contain properties as defined by <see cref="LocalizedEntityDescriptor.Properties"/>,
        /// plus the <see cref="BaseEntity.Id"/> and <c>KeyGroup</c> properties.
        /// </summary>
        /// <param name="descriptor">The descriptor that contains metadata about the data to load.</param>
        /// <returns>A list of dynamic entities.</returns>
        IList<dynamic> Load(LocalizedEntityDescriptor descriptor);

        /// <inheritdoc cref="Load(LocalizedEntityDescriptor)"/>
        Task<IList<dynamic>> LoadAsync(LocalizedEntityDescriptor descriptor);

        /// <summary>
        /// Loads dynamically shaped entities for given <paramref name="descriptor"/> as a paged list.
        /// The dynamic instances only contain properties as defined by <see cref="LocalizedEntityDescriptor.Properties"/>,
        /// plus the <see cref="BaseEntity.Id"/> and <c>KeyGroup</c> properties.
        /// </summary>
        /// <param name="descriptor">The descriptor that contains metadata about the data to load.</param>
        /// <param name="pageSize">Size of paged data</param>
        /// <returns>The <see cref="DynamicFastPager"/> instead used to iterate through all data pages.</returns>
        DynamicFastPager LoadPaged(LocalizedEntityDescriptor descriptor, int pageSize = 1000);

        /// <summary>
        /// Loads localized entities by calling the given <paramref name="delegate"/>.
        /// </summary>
        Task<IList<dynamic>> LoadByDelegateAsync(LoadLocalizedEntityDelegate @delegate);
    }
}
