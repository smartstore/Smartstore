using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Checkout.Attributes
{
    /// <summary>
    /// Represents a checkout attribute
    /// </summary>
    [CacheableEntity]
    [LocalizedEntity("IsActive")]
    public partial class CheckoutAttribute : EntityWithAttributes, ILocalizedEntity, IStoreRestricted
    {
        public CheckoutAttribute()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private CheckoutAttribute(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether the checkout attribute is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        [Required, MaxLength(400)]
        [LocalizedProperty]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the text prompt
        /// </summary>
        [LocalizedProperty]
        public string TextPrompt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is required
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether shippable products are required in order to display this attribute
        /// </summary>
        public bool ShippableProductRequired { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the attribute is marked as tax exempt
        /// </summary>
        public bool IsTaxExempt { get; set; }

        /// <summary>
        /// Gets or sets the tax category identifier
        /// </summary>
        public int TaxCategoryId { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is limited/restricted to certain stores
        /// </summary>
        public bool LimitedToStores { get; set; }

        /// <summary>
        /// Gets or sets the attribute control type identifier
        /// </summary>
        public int AttributeControlTypeId { get; set; }

        /// <summary>
        /// Gets the attribute control type
        /// </summary>
        [NotMapped]
        public AttributeControlType AttributeControlType
        {
            get => (AttributeControlType)AttributeControlTypeId;
            set => AttributeControlTypeId = (int)value;
        }

        /// <summary>
        /// Gets a value indicating whether the attribute has a list of values.
        /// </summary>
        public bool IsListTypeAttribute => AttributeControlType switch
        {
            AttributeControlType.Checkboxes
            or AttributeControlType.Boxes
            or AttributeControlType.DropdownList
            or AttributeControlType.RadioList => true,
            _ => false
        };

        private ICollection<CheckoutAttributeValue> _checkoutAttributeValues;
        /// <summary>
        /// Gets or sets the checkout attributes collection
        /// </summary>
        public ICollection<CheckoutAttributeValue> CheckoutAttributeValues
        {
            get => _checkoutAttributeValues ?? LazyLoader.Load(this, ref _checkoutAttributeValues) ?? (_checkoutAttributeValues ??= new HashSet<CheckoutAttributeValue>());
            protected set => _checkoutAttributeValues = value;
        }
    }
}