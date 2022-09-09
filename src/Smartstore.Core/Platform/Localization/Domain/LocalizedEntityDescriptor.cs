namespace Smartstore.Core.Localization
{
    /// <summary>
    /// Contains metadata for localized entities.
    /// </summary>
    public class LocalizedEntityDescriptor
    {
        private string _keyGroup;
        
        /// <summary>
        /// Type of localized entity that implements <see cref="ILocalizedEntity"/>.
        /// </summary>
        public Type EntityType { get; init; }

        public string KeyGroup 
        {
            get => _keyGroup ?? EntityType.Name;
            init => _keyGroup = value;
        }

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
