namespace Smartstore.Core.Localization
{
    /// <summary>
    /// Contains metadata for localized entities.
    /// </summary>
    public class LocalizedEntityDescriptor
    {
        /// <summary>
        /// Type of localized entity that implements <see cref="ILocalizedEntity"/>.
        /// </summary>
        public Type EntityType { get; init; }

        /// <summary>
        /// Name of all localizable properties that are defined in <see cref="EntityType"/> class.
        /// </summary>
        public string[] PropertyNames { get; init; }

        /// <summary>
        /// An optional filter predicate as a dynamic LINQ expression.
        /// </summary>
        public string FilterPredicate { get; set; }
    }
}
