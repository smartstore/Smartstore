using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Identity;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Rules
{
    /// <summary>
    /// Represents a set of rules.
    /// </summary>
    [Table("RuleSet")]
    [Index(nameof(IsSubGroup), Name = "IX_IsSubGroup")]
    [Index(nameof(IsActive), nameof(Scope), Name = "IX_RuleSetEntity_Scope")]
    [CacheableEntity(MaxRows = 1, Expiry = 480)] // MaxRows = 1 caches only ById calls
    public partial class RuleSetEntity : EntityWithAttributes, IAuditable
    {
        public RuleSetEntity()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private RuleSetEntity(ILazyLoader lazyLoader) : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the name of the rule set.
        /// </summary>
        [StringLength(200)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [StringLength(400)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the rule set is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the scope of the rule set.
        /// </summary>
        [Required]
        public RuleScope Scope { get; set; }

        /// <summary>
        /// <c>True</c> when this set is an internal composite container for rules within another rule set.
        /// </summary>
        public bool IsSubGroup { get; set; }

        /// <summary>
        /// Gets or sets the logical operator for the rules in this set.
        /// </summary>
        public LogicalRuleOperator LogicalOperator { get; set; }

        /// <inheritdoc/>
        public DateTime CreatedOnUtc { get; set; }

        /// <inheritdoc/>
        public DateTime UpdatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the date when the set was processed.
        /// </summary>
        public DateTime? LastProcessedOnUtc { get; set; }

        private ICollection<RuleEntity> _rules;
        /// <summary>
        /// Gets rules
        /// </summary>
        public ICollection<RuleEntity> Rules
        {
            get => _rules ?? LazyLoader.Load(this, ref _rules) ?? (_rules ??= new HashSet<RuleEntity>());
            protected set => _rules = value;
        }

        private ICollection<Discount> _discounts;
        /// <summary>
        /// Gets or sets assigned discounts.
        /// </summary>
        public ICollection<Discount> Discounts
        {
            get => _discounts ?? LazyLoader.Load(this, ref _discounts) ?? (_discounts ??= new HashSet<Discount>());
            protected set => _discounts = value;
        }

        private ICollection<Category> _categories;
        /// <summary>
        /// Gets or sets assigned categories.
        /// </summary>
        public ICollection<Category> Categories
        {
            get => _categories ?? LazyLoader.Load(this, ref _categories) ?? (_categories ??= new HashSet<Category>());
            protected set => _categories = value;
        }

        private ICollection<ShippingMethod> _shippingMethods;
        /// <summary>
        /// Gets or sets assigned shipping methods.
        /// </summary>
        public ICollection<ShippingMethod> ShippingMethods
        {
            get => _shippingMethods ?? LazyLoader.Load(this, ref _shippingMethods) ?? (_shippingMethods ??= new HashSet<ShippingMethod>());
            protected set => _shippingMethods = value;
        }

        private ICollection<PaymentMethod> _paymentMethods;
        /// <summary>
        /// Gets or sets assigned payment methods.
        /// </summary>
        public ICollection<PaymentMethod> PaymentMethods
        {
            get => _paymentMethods ?? LazyLoader.Load(this, ref _paymentMethods) ?? (_paymentMethods ??= new HashSet<PaymentMethod>());
            protected set => _paymentMethods = value;
        }

        private ICollection<CustomerRole> _customerRoles;
        /// <summary>
        /// Gets or sets assigned customer roles.
        /// </summary>
        public ICollection<CustomerRole> CustomerRoles
        {
            get => _customerRoles ?? LazyLoader.Load(this, ref _customerRoles) ?? (_customerRoles ??= new HashSet<CustomerRole>());
            protected set => _customerRoles = value;
        }
    }
}
