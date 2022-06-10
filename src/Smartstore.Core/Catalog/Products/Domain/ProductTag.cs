using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Catalog.Products
{
    /// <summary>
    /// Represents a product tag.
    /// </summary>
    [Index(nameof(Name), Name = "IX_ProductTag_Name")]
    [Index(nameof(Published), Name = "IX_ProductTag_Published")]
    public partial class ProductTag : BaseEntity, ILocalizedEntity, IDisplayedEntity
    {
        public ProductTag()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private ProductTag(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        public string GetDisplayNameMemberName() => nameof(Name);
        public string GetDisplayName() => Name;

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [Required, StringLength(400)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is published.
        /// </summary>
        public bool Published { get; set; } = true;

        private ICollection<Product> _products;
        /// <summary>
        /// Gets or sets the products.
        /// </summary>
        [JsonIgnore]
        public ICollection<Product> Products
        {
            get => _products ?? LazyLoader.Load(this, ref _products) ?? (_products ??= new HashSet<Product>());
            protected set => _products = value;
        }
    }
}
