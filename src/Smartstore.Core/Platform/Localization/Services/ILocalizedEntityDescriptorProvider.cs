using System.Runtime.CompilerServices;
using Autofac;
using Smartstore.Core.Data;

namespace Smartstore.Core.Localization
{
    public delegate Task<IList<dynamic>> LoadLocalizedEntityDelegate(ILifetimeScope scope, SmartDbContext db);

    /// <summary>
    /// Options for the <see cref="LocalizedEntityDescriptorProvider"/>
    /// </summary>
    public class LocalizedEntityOptions
    {
        /// <summary>
        /// A list of <see cref="LoadLocalizedEntityDelegate"/>s used to load data from any custom source.
        /// Returned dynamic objects MUST contain the <c>Id</c> property as <see cref="int"/> and 
        /// the <c>KeyGroup</c> property as <see cref="string"/> alongside the actual
        /// localizable properties.
        /// </summary>
        public IList<LoadLocalizedEntityDelegate> Delegates { get; } = new List<LoadLocalizedEntityDelegate>();
    }

    /// <summary>
    /// Responsible for determining localized entity metadata for all active entity types,
    /// and for determining load delegates.
    /// </summary>
    public interface ILocalizedEntityDescriptorProvider
    {
        /// <summary>
        /// Gets a descriptor list of all localized entities that implement <see cref="ILocalizedEntity"/>
        /// and decorate at least one property with the <see cref="LocalizedEntityAttribute"/> attribute.
        /// Key is the entity type.
        /// </summary>
        IReadOnlyDictionary<Type, LocalizedEntityDescriptor> GetDescriptors();

        /// <summary>
        /// Gets a list of all delegates that can load localized entity data from any custom source.
        /// Delegates can be registered by adding them to <see cref="LocalizedEntityOptions.Delegates"/>.
        /// </summary>
        IReadOnlyList<LoadLocalizedEntityDelegate> GetDelegates();
    }

    public static class ILocalizedEntityDescriptorProviderExtensions 
    {
        /// <summary>
        /// Gets a descriptor by given <paramref name="entityType"/>.
        /// </summary>
        /// <returns>The descriptor instance or <c>null</c> if not found.</returns>
        public static LocalizedEntityDescriptor GetDescriptorByEntityType(this ILocalizedEntityDescriptorProvider provider, Type entityType)
        {
            Guard.NotNull(entityType, nameof(entityType));

            if (provider.GetDescriptors().TryGetValue(entityType, out var descriptor))
            {
                return descriptor;
            }

            return null;
        }
    }
}
