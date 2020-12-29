using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Data.Caching;
using Smartstore.Domain;

namespace Smartstore.Core.Checkout.Attributes
{
    /// <summary>
    /// Represents a checkout attribute
    /// </summary>
    [CacheableEntity]
    public partial class CheckoutAttribute : BaseEntity, ILocalizedEntity, IStoreRestricted
    {
        private readonly ILazyLoader _lazyLoader;

        public CheckoutAttribute()
        {
        }

        public CheckoutAttribute(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the checkout attribute is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        [Required, MaxLength(400)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the text prompt
        /// </summary>
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
            get => _lazyLoader?.Load(this, ref _checkoutAttributeValues) ?? (_checkoutAttributeValues ??= new HashSet<CheckoutAttributeValue>());
            protected set => _checkoutAttributeValues = value;
        }
    }
}