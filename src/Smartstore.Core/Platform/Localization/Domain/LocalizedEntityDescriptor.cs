using System.Reflection;
using Smartstore.Core.Seo;

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
            get => _keyGroup ?? NamedEntity.GetEntityName(EntityType);
            init => _keyGroup = value;
        }

        /// <summary>
        /// All localizable properties that are defined in <see cref="EntityType"/> class,
        /// that is, all properties decorated with the <see cref="LocalizedEntityAttribute"/> attribute.
        /// </summary>
        public PropertyInfo[] Properties { get; init; }

        /// <summary>
        /// An optional filter predicate as a dynamic LINQ expression.
        /// </summary>
        public string FilterPredicate { get; set; }
    }
}
