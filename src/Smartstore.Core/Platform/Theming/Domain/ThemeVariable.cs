using System.ComponentModel.DataAnnotations;

namespace Smartstore.Core.Theming
{
    public class ThemeVariable : BaseEntity
    {
        /// <summary>
        /// Gets or sets the theme the variable belongs to
        /// </summary>
        [StringLength(400)]
        public string Theme { get; set; }

        /// <summary>
        /// Gets or sets the theme attribute name
        /// </summary>
        [StringLength(400)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the theme attribute value
        /// </summary>
        [StringLength(2000)]
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the store identifier
        /// </summary>
        public int StoreId { get; set; }

        protected override bool Equals(BaseEntity other)
        {
            var equals = base.Equals(other);
            if (!equals)
            {
                var o2 = other as ThemeVariable;
                if (o2 != null)
                {
                    equals = Theme.EqualsNoCase(o2.Theme) && Name == o2.Name;
                }
            }
            return equals;
        }
    }
}
