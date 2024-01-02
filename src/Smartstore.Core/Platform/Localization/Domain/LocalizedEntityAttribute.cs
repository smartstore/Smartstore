namespace Smartstore.Core.Localization
{
    /// <summary>
    /// Defines extra metadata for localizable entities. Use this attribute
    /// on types that implement <see cref="ILocalizedEntity"/> only.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = true)]
    public sealed class LocalizedEntityAttribute : Attribute
    {
        public LocalizedEntityAttribute()
        {
        }

        /// <param name="filterPredicate">
        /// An optional filter predicate as a dynamic LINQ expression
        /// </param>
        public LocalizedEntityAttribute(string filterPredicate)
        {
            Guard.NotEmpty(filterPredicate);
            FilterPredicate = filterPredicate;
        }

        /// <summary>
        /// An optional filter predicate as a dynamic LINQ expression.
        /// </summary>
        public string FilterPredicate { get; set; }

        /// <summary>
        /// Key group name of entity.
        /// </summary>
        public string KeyGroup { get; set; }
    }

    /// <summary>
    /// Marks a public property of a localizable entity as localizable. 
    /// Use this attribute on types that implement <see cref="ILocalizedEntity"/> only.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public sealed class LocalizedPropertyAttribute : Attribute
    {
        public bool Translatable { get; set; } = true;
    }
}
