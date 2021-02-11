using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Domain;

namespace Smartstore.Core.Catalog.Discounts
{
    public abstract class EntityWithDiscounts : EntityWithAttributes, IDiscountable
    {
        private ICollection<Discount> _appliedDiscounts;

        protected EntityWithDiscounts()
        {
        }

        protected EntityWithDiscounts(ILazyLoader lazyLoader)
        {
            LazyLoader = lazyLoader;
        }

        [NotMapped]
        protected ILazyLoader LazyLoader { get; }

        /// <inheritdoc />
        public bool HasDiscountsApplied { get; set; }

        /// <summary>
        /// Gets or sets the applied discounts.
        /// </summary>
        public ICollection<Discount> AppliedDiscounts
        {
            get => LazyLoader?.Load(this, ref _appliedDiscounts) ?? (_appliedDiscounts ??= new HashSet<Discount>());
            protected set => _appliedDiscounts = value;
        }
    }
}