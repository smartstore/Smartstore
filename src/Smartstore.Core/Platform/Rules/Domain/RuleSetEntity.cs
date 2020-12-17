using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Customers;
using Smartstore.Data.Caching;
using Smartstore.Domain;

namespace Smartstore.Core.Rules
{
    [Table("RuleSet")]
    [Index(nameof(IsSubGroup), Name = "IX_IsSubGroup")]
    [Index(nameof(IsActive), nameof(Scope), Name = "IX_RuleSetEntity_Scope")]
    [CacheableEntity(MaxRows = 1, Expiry = 480)] // MaxRows = 1 caches only ById calls
    public partial class RuleSetEntity : BaseEntity, IAuditable
    {
        private readonly ILazyLoader _lazyLoader;

        public RuleSetEntity()
        {
        }

        public RuleSetEntity(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
        }

        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(400)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        public RuleScope Scope { get; set; }

        /// <summary>
        /// True when this set is an internal composite container for rules within another ruleset.
        /// </summary>
        public bool IsSubGroup { get; set; }

        public LogicalRuleOperator LogicalOperator { get; set; }

        public DateTime CreatedOnUtc { get; set; }

        public DateTime UpdatedOnUtc { get; set; }

        public DateTime? LastProcessedOnUtc { get; set; }

        private ICollection<RuleEntity> _rules;
        /// <summary>
        /// Gets rules
        /// </summary>
        public ICollection<RuleEntity> Rules
        {
            get => _lazyLoader?.Load(this, ref _rules) ?? (_rules ??= new HashSet<RuleEntity>());
            protected set => _rules = value;
        }

        private ICollection<Discount> _discounts;
        /// <summary>
        /// Gets or sets assigned discounts.
        /// </summary>
        public ICollection<Discount> Discounts
        {
            get => _lazyLoader?.Load(this, ref _discounts) ?? (_discounts ??= new HashSet<Discount>());
            protected set => _discounts = value;
        }

        private ICollection<Category> _categories;
        /// <summary>
        /// Gets or sets assigned categories.
        /// </summary>
        public ICollection<Category> Categories
        {
            get => _lazyLoader?.Load(this, ref _categories) ?? (_categories ??= new HashSet<Category>());
            protected set => _categories = value;
        }

        //// TODO: (mg) (core) RuleSetEntity > implement missing nav props: ShippingMethods, PaymentMethods

        //private ICollection<ShippingMethod> _shippingMethods;
        ///// <summary>
        ///// Gets or sets assigned shipping methods.
        ///// </summary>
        //public ICollection<ShippingMethod> ShippingMethods
        //{
        //    get => _shippingMethods ?? (_shippingMethods = new HashSet<ShippingMethod>());
        //    protected set => _shippingMethods = value;
        //}

        //private ICollection<PaymentMethod> _paymentMethods;
        ///// <summary>
        ///// Gets or sets assigned payment methods.
        ///// </summary>
        //public ICollection<PaymentMethod> PaymentMethods
        //{
        //    get => _paymentMethods ?? (_paymentMethods = new HashSet<PaymentMethod>());
        //    protected set => _paymentMethods = value;
        //}

        private ICollection<CustomerRole> _customerRoles;
        /// <summary>
        /// Gets or sets assigned customer roles.
        /// </summary>
        public ICollection<CustomerRole> CustomerRoles
        {
            get => _customerRoles ?? (_customerRoles = new HashSet<CustomerRole>());
            protected set => _customerRoles = value;
        }
    }
}
