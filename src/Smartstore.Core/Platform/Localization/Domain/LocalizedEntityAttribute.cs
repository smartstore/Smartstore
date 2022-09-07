namespace Smartstore.Core.Localization
{
    /// <summary>
    /// Defines metadata for localizable entities. Use this attribute
    /// on types that implement <see cref="ILocalizedEntity"/> only.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class LocalizedEntityAttribute : Attribute
    {
        public LocalizedEntityAttribute(params string[] propertyNames)
        {
            Guard.NotEmpty(propertyNames, nameof(propertyNames));
            PropertyNames = propertyNames;
        }

        /// <summary>
        /// The properties which provide localizable content.
        /// </summary>
        public string[] PropertyNames { get; }

        /// <summary>
        /// An optional filter predicate as a dynamic LINQ expression.
        /// </summary>
        public string FilterPredicate { get; set; }

        internal LocalizedEntityDescriptor ToDescriptor(Type entityType)
        {
            return new LocalizedEntityDescriptor
            {
                EntityType = entityType,
                PropertyNames = PropertyNames,
                FilterPredicate = FilterPredicate
            };
        }
    }
}
