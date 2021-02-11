using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Domain;

namespace Smartstore.Core.Catalog.Discounts
{
    public abstract class EntityWithDiscounts : EntityWithAttributes, IDiscountable
    {
        private readonly ILazyLoader _lazyLoader;
        private ICollection<Discount> _appliedDiscounts;

        protected EntityWithDiscounts()
        {
        }

        protected EntityWithDiscounts(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
        }

        /// <inheritdoc />
        public bool HasDiscountsApplied { get; set; }

        /// <summary>
        /// Gets or sets the applied discounts.
        /// </summary>
        public ICollection<Discount> AppliedDiscounts
        {
            get => _lazyLoader?.Load(this, ref _appliedDiscounts) ?? (_appliedDiscounts ??= new HashSet<Discount>());
            protected set => _appliedDiscounts = value;
        }
    }
}