using System.Linq.Dynamic.Core;
using AngleSharp.Dom;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Search;

namespace Smartstore.Core.Catalog.Search
{
    public class CatalogSearchQueryVisitor : LinqSearchQueryVisitor<Product, CatalogSearchQuery, CatalogSearchQueryContext>
    {
        private static readonly string[] _searchTermFields = new[]
        {
            CatalogSearchQuery.KnownFilters.Name,
            CatalogSearchQuery.KnownFilters.Sku,
            CatalogSearchQuery.KnownFilters.ShortDescription
        };

        protected override IQueryable<Product> VisitFilter(ISearchFilter filter, CatalogSearchQueryContext context, IQueryable<Product> query)
        {
            var names = CatalogSearchQuery.KnownFilters;
            var fieldName = filter.FieldName;

            if (_searchTermFields.Contains(fieldName) || fieldName == "searchterm")
            {
                return VisitTermFilter(filter, context, query);
            }
            else if (fieldName == names.ProductId)
            {
                return ApplySimpleMemberExpression(x => x.Id, filter, query);
            }
            else if (fieldName == names.StockQuantity)
            {
                return ApplySimpleMemberExpression(x => x.StockQuantity, filter, query);
            }
            else if (fieldName == names.ParentId)
            {
                return ApplySimpleMemberExpression(x => x.ParentGroupedProductId, filter, query);
            }
            else if (fieldName == names.CreatedOn)
            {
                return ApplySimpleMemberExpression(x => x.CreatedOnUtc, filter, query);
            }
            else if (fieldName == names.IsPublished)
            {
                return ApplySimpleMemberExpression(x => x.Published, filter, query);
            }
            else if (fieldName == names.ShowOnHomepage)
            {
                return ApplySimpleMemberExpression(x => x.ShowOnHomePage, filter, query);
            }
            else if (fieldName == names.IsDownload)
            {
                return ApplySimpleMemberExpression(x => x.IsDownload, filter, query);
            }
            else if (fieldName == names.IsRecurring)
            {
                return ApplySimpleMemberExpression(x => x.IsRecurring, filter, query);
            }
            else if (fieldName == names.IsShippingEnabled)
            {
                return ApplySimpleMemberExpression(x => x.IsShippingEnabled, filter, query);
            }
            else if (fieldName == names.IsFreeShipping)
            {
                return ApplySimpleMemberExpression(x => x.IsFreeShipping, filter, query);
            }
            else if (fieldName == names.IsTaxExempt)
            {
                return ApplySimpleMemberExpression(x => x.IsTaxExempt, filter, query);
            }
            else if (fieldName == names.IsEsd)
            {
                return ApplySimpleMemberExpression(x => x.IsEsd, filter, query);
            }
            else if (fieldName == names.HasDiscount)
            {
                return ApplySimpleMemberExpression(x => x.HasDiscountsApplied, filter, query);
            }
            else if (fieldName == names.IsAvailable)
            {
                return query.Where(x =>
                    x.ManageInventoryMethodId == (int)ManageInventoryMethod.DontManageStock ||
                    (x.ManageInventoryMethodId == (int)ManageInventoryMethod.ManageStock && (x.StockQuantity > 0 || x.BackorderModeId != (int)BackorderMode.NoBackorders)) ||
                    (x.ManageInventoryMethodId == (int)ManageInventoryMethod.ManageStockByAttributes && x.ProductVariantAttributeCombinations.Any(pvac => pvac.StockQuantity > 0 || pvac.AllowOutOfStockOrders))
                );
            }
            else if (fieldName == names.DeliveryId)
            {
                var deliverTimeIds = filter.GetTermsArray<int>();
                if (deliverTimeIds.Length == 1)
                {
                    return query.Where(x => x.DeliveryTimeId != null && x.DeliveryTimeId == deliverTimeIds[0]);
                }
                else if (deliverTimeIds.Length > 1)
                {
                    return query.Where(x => x.DeliveryTimeId != null && deliverTimeIds.Contains(x.DeliveryTimeId.Value));
                }
            }
            else if (fieldName == names.Condition)
            {
                var conditions = filter.GetTermsArray<int>();
                if (conditions.Length == 1)
                {
                    return query.Where(x => (int)x.Condition == conditions[0]);
                }
                else if (conditions.Length > 1)
                {
                    return query.Where(x => conditions.Contains((int)x.Condition));
                }
            }
            else if (fieldName == names.Rating)
            {
                return VisitRatingFilter(filter as IAttributeSearchFilter, context, query);
            }
            else if (fieldName == names.RoleId)
            {
                return VisitRoleFilter(filter, context, query);
            }
            else if (fieldName == names.StoreId)
            {
                return VisitStoreFilter(filter, context, query);
            }
            else if (fieldName.StartsWith(names.Price))
            {
                return VisitPriceFilter(filter as IAttributeSearchFilter, context, query);
            }
            else if (fieldName.EndsWith(names.CategoryId))
            {
                if (filter is IRangeSearchFilter rf)
                {
                    // Has any category.
                    if (1 == ((rf.Term as int?) ?? 0) && int.MaxValue == ((rf.UpperTerm as int?) ?? 0))
                    {
                        return query.Where(x => x.ProductCategories.Count > 0);
                    }
                }
                else
                {
                    var categoryIds = filter.GetTermsArray<int>();

                    if (categoryIds.Length > 0)
                    {
                        bool? featuredOnly = filter.FieldName == names.CategoryId ? null : filter.FieldName.StartsWith("featured");
                        context.CategoryId ??= categoryIds.First();

                        if (featuredOnly.HasValue)
                        {
                            context.IsGroupingRequired = true;
                            return ApplyCategoriesFilter(query, categoryIds, featuredOnly);
                        }
                        else
                        {
                            if (categoryIds.Length == 1 && context.CategoryId == 0)
                            {
                                // Has no category.
                                return query.Where(x => x.ProductCategories.Count == 0);
                            }
                            else
                            {
                                context.IsGroupingRequired = true;
                                return ApplyCategoriesFilter(query, categoryIds, null);
                            }
                        }
                    }
                }
            }
            else if (fieldName.EndsWith(names.ManufacturerId))
            {
                if (filter is IRangeSearchFilter rf)
                {
                    // Has any manufacturer.
                    if (1 == ((rf.Term as int?) ?? 0) && int.MaxValue == ((rf.UpperTerm as int?) ?? 0))
                    {
                        return query.Where(x => x.ProductManufacturers.Count > 0);
                    }
                }
                else
                {
                    var manufacturerIds = filter.GetTermsArray<int>();
                    if (manufacturerIds.Length > 0)
                    {
                        bool? featuredOnly = filter.FieldName == names.ManufacturerId ? null : filter.FieldName.StartsWith("featured");
                        context.ManufacturerId ??= manufacturerIds.First();

                        if (featuredOnly.HasValue)
                        {
                            context.IsGroupingRequired = true;
                            return ApplyManufacturersFilter(query, manufacturerIds, featuredOnly);
                        }
                        else
                        {
                            if (manufacturerIds.Length == 1 && context.ManufacturerId == 0)
                            {
                                // Has no manufacturer.
                                return query.Where(x => x.ProductManufacturers.Count == 0);
                            }
                            else
                            {
                                context.IsGroupingRequired = true;
                                return ApplyManufacturersFilter(query, manufacturerIds, null);
                            }
                        }
                    }
                }
            }
            else if (fieldName == names.TagId)
            {
                var tagIds = filter.GetTermsArray<int>();
                if (tagIds.Length > 0)
                {
                    context.IsGroupingRequired = true;
                    return
                        from p in query
                        from pt in p.ProductTags.Where(pt => tagIds.Contains(pt.Id))
                        select p;
                }
            }
            else if (filter is IRangeSearchFilter rf)
            {
                // Range only filters
                if (fieldName == names.AvailableStart)
                {
                    var lower = rf.Term as DateTime?;
                    var upper = rf.UpperTerm as DateTime?;

                    if (lower.HasValue)
                    {
                        if (rf.IncludesLower)
                            query = query.Where(x => !x.AvailableStartDateTimeUtc.HasValue || x.AvailableStartDateTimeUtc >= lower.Value);
                        else
                            query = query.Where(x => !x.AvailableStartDateTimeUtc.HasValue || x.AvailableStartDateTimeUtc > lower.Value);
                    }

                    if (upper.HasValue)
                    {
                        if (rf.IncludesLower)
                            query = query.Where(x => !x.AvailableStartDateTimeUtc.HasValue || x.AvailableStartDateTimeUtc <= upper.Value);
                        else
                            query = query.Where(x => !x.AvailableStartDateTimeUtc.HasValue || x.AvailableStartDateTimeUtc < upper.Value);
                    }

                    return query;
                }
                else if (fieldName == names.AvailableEnd)
                {
                    var lower = rf.Term as DateTime?;
                    var upper = rf.UpperTerm as DateTime?;

                    if (lower.HasValue)
                    {
                        if (rf.IncludesLower)
                            query = query.Where(x => !x.AvailableEndDateTimeUtc.HasValue || x.AvailableEndDateTimeUtc >= lower.Value);
                        else
                            query = query.Where(x => !x.AvailableEndDateTimeUtc.HasValue || x.AvailableEndDateTimeUtc > lower.Value);
                    }

                    if (upper.HasValue)
                    {
                        if (rf.IncludesLower)
                            query = query.Where(x => !x.AvailableEndDateTimeUtc.HasValue || x.AvailableEndDateTimeUtc <= upper.Value);
                        else
                            query = query.Where(x => !x.AvailableEndDateTimeUtc.HasValue || x.AvailableEndDateTimeUtc < upper.Value);
                    }

                    return query;
                }
            }
            else if (filter is CategoryTreePathFilter tpf)
            {
                context.IsGroupingRequired = true;
                context.CategoryId ??= tpf.CategoryId;

                if (tpf.IncludeSelf)
                {
                    return
                        from p in query
                        from pc in p.ProductCategories.Where(x => x.Category.TreePath.StartsWith(tpf.TreePath))
                        where !tpf.FeaturedOnly.HasValue || tpf.FeaturedOnly.Value == pc.IsFeaturedProduct
                        select p;
                }
                else
                {
                    return
                        from p in query
                        from pc in p.ProductCategories.Where(x => x.Category.TreePath.StartsWith(tpf.TreePath) && x.Category.TreePath.Length > tpf.TreePath.Length)
                        where !tpf.FeaturedOnly.HasValue || tpf.FeaturedOnly.Value == pc.IsFeaturedProduct
                        select p;
                }
            }
            else if (filter is IAttributeSearchFilter af)
            {
                // Attribute filters except range filters
                if (fieldName == names.TypeId)
                {
                    return query.Where(x => x.ProductTypeId == (int)af.Term);
                }
                else if (fieldName == names.Visibility)
                {
                    var visibility = (ProductVisibility)af.Term;
                    return visibility switch
                    {
                        ProductVisibility.SearchResults => query.Where(x => x.Visibility <= visibility),
                        _ => query.Where(x => x.Visibility == visibility),
                    };
                }
            }

            return query;
        }

        protected virtual IQueryable<Product> VisitTermFilter(ISearchFilter filter, CatalogSearchQueryContext context, IQueryable<Product> query)
        {
            var op = LogicalRuleOperator.And;
            IAttributeSearchFilter[] filters = null;

            if (filter is ICombinedSearchFilter cf)
            {
                op = LogicalRuleOperator.Or;
                filters = cf.Filters.OfType<IAttributeSearchFilter>().ToArray();
            }
            else if (filter is IAttributeSearchFilter af)
            {
                filters = new[] { af };
            }

            if (filters.IsNullOrEmpty())
            {
                return query;
            }

            var names = CatalogSearchQuery.KnownFilters;
            var languageId = context.SearchQuery.LanguageId ?? 0;

            var baseQuery =
                from p in query
                join lp in context.Services.DbContext.LocalizedProperties on p.Id equals lp.EntityId into plp
                from lp in plp.DefaultIfEmpty()
                select new TermSearchProduct { Product = p, Translation = lp };

            var expressions = filters
                .Select(af =>
                {
                    if (af.FieldName == names.Sku)
                    {
                        return TermSearchProduct.CreateFilter(x => x.Product.Sku, af);
                    }
                    else if (af.FieldName == names.Name)
                    {
                        return TermSearchProduct.CreateFilter(x => x.Product.Name, af, languageId);
                    }
                    else if (af.FieldName == names.ShortDescription)
                    {
                        return TermSearchProduct.CreateFilter(x => x.Product.ShortDescription, af, languageId);
                    }

                    return null;
                })
                .Where(x => x != null)
                .ToArray();

            if (expressions.IsNullOrEmpty())
            {
                return query;
            }

            context.IsGroupingRequired = true;

            var group = expressions.Length == 1 && expressions[0] is FilterExpressionGroup group2
                ? group2
                : new FilterExpressionGroup(typeof(TermSearchProduct), expressions.ToArray()) { LogicalOperator = op };

            baseQuery = baseQuery.Where(group).Cast<TermSearchProduct>();

            return baseQuery.Select(x => x.Product);
        }

        protected virtual IQueryable<Product> VisitPriceFilter(IAttributeSearchFilter filter, CatalogSearchQueryContext context, IQueryable<Product> query)
        {
            if (filter is IRangeSearchFilter rf)
            {
                var lower = rf.Term as double?;
                var upper = rf.UpperTerm as double?;

                if (lower.HasValue)
                {
                    var minPrice = Convert.ToDecimal(lower.Value);

                    query = query.Where(x =>
                        ((x.SpecialPrice.HasValue &&
                        ((!x.SpecialPriceStartDateTimeUtc.HasValue || x.SpecialPriceStartDateTimeUtc.Value < context.Now) &&
                        (!x.SpecialPriceEndDateTimeUtc.HasValue || x.SpecialPriceEndDateTimeUtc.Value > context.Now))) &&
                        (x.SpecialPrice >= minPrice))
                        ||
                        ((!x.SpecialPrice.HasValue ||
                        ((x.SpecialPriceStartDateTimeUtc.HasValue && x.SpecialPriceStartDateTimeUtc.Value > context.Now) ||
                        (x.SpecialPriceEndDateTimeUtc.HasValue && x.SpecialPriceEndDateTimeUtc.Value < context.Now))) &&
                        (x.Price >= minPrice))
                    );
                }

                if (upper.HasValue)
                {
                    var maxPrice = Convert.ToDecimal(upper);

                    query = query.Where(x =>
                        ((x.SpecialPrice.HasValue &&
                        ((!x.SpecialPriceStartDateTimeUtc.HasValue || x.SpecialPriceStartDateTimeUtc.Value < context.Now) &&
                        (!x.SpecialPriceEndDateTimeUtc.HasValue || x.SpecialPriceEndDateTimeUtc.Value > context.Now))) &&
                        (x.SpecialPrice <= maxPrice))
                        ||
                        ((!x.SpecialPrice.HasValue ||
                        ((x.SpecialPriceStartDateTimeUtc.HasValue && x.SpecialPriceStartDateTimeUtc.Value > context.Now) ||
                        (x.SpecialPriceEndDateTimeUtc.HasValue && x.SpecialPriceEndDateTimeUtc.Value < context.Now))) &&
                        (x.Price <= maxPrice))
                    );
                }
            }
            else
            {
                var price = Convert.ToDecimal(filter.Term);

                if (filter.Occurence == SearchFilterOccurence.MustNot)
                {
                    query = query.Where(x =>
                        ((x.SpecialPrice.HasValue &&
                        ((!x.SpecialPriceStartDateTimeUtc.HasValue || x.SpecialPriceStartDateTimeUtc.Value < context.Now) &&
                        (!x.SpecialPriceEndDateTimeUtc.HasValue || x.SpecialPriceEndDateTimeUtc.Value > context.Now))) &&
                        (x.SpecialPrice != price))
                        ||
                        ((!x.SpecialPrice.HasValue ||
                        ((x.SpecialPriceStartDateTimeUtc.HasValue && x.SpecialPriceStartDateTimeUtc.Value > context.Now) ||
                        (x.SpecialPriceEndDateTimeUtc.HasValue && x.SpecialPriceEndDateTimeUtc.Value < context.Now))) &&
                        (x.Price != price))
                    );
                }
                else
                {
                    query = query.Where(x =>
                        ((x.SpecialPrice.HasValue &&
                        ((!x.SpecialPriceStartDateTimeUtc.HasValue || x.SpecialPriceStartDateTimeUtc.Value < context.Now) &&
                        (!x.SpecialPriceEndDateTimeUtc.HasValue || x.SpecialPriceEndDateTimeUtc.Value > context.Now))) &&
                        (x.SpecialPrice == price))
                        ||
                        ((!x.SpecialPrice.HasValue ||
                        ((x.SpecialPriceStartDateTimeUtc.HasValue && x.SpecialPriceStartDateTimeUtc.Value > context.Now) ||
                        (x.SpecialPriceEndDateTimeUtc.HasValue && x.SpecialPriceEndDateTimeUtc.Value < context.Now))) &&
                        (x.Price == price))
                    );
                }
            }

            return query;
        }

        protected virtual IQueryable<Product> VisitRatingFilter(IAttributeSearchFilter filter, CatalogSearchQueryContext context, IQueryable<Product> query)
        {
            if (filter is IRangeSearchFilter rf)
            {
                var lower = rf.Term as double?;
                var upper = rf.UpperTerm as double?;

                if (lower.HasValue || upper.HasValue)
                {
                    query = query.Where(x => x.ApprovedTotalReviews > 0);
                }

                if (lower.HasValue)
                {
                    if (rf.IncludesLower)
                        query = query.Where(x => ((double)x.ApprovedRatingSum / (double)x.ApprovedTotalReviews) >= lower.Value);
                    else
                        query = query.Where(x => ((double)x.ApprovedRatingSum / (double)x.ApprovedTotalReviews) > lower.Value);
                }

                if (upper.HasValue)
                {
                    if (rf.IncludesUpper)
                        query = query.Where(x => ((double)x.ApprovedRatingSum / (double)x.ApprovedTotalReviews) <= upper.Value);
                    else
                        query = query.Where(x => ((double)x.ApprovedRatingSum / (double)x.ApprovedTotalReviews) < upper.Value);
                }
            }
            else
            {
                var rating = Convert.ToDouble(filter.Term);

                if (filter.Occurence == SearchFilterOccurence.MustNot)
                {
                    query = query.Where(x => x.ApprovedTotalReviews > 0 && ((double)x.ApprovedRatingSum / (double)x.ApprovedTotalReviews) != rating);
                }
                else
                {
                    query = query.Where(x => x.ApprovedTotalReviews > 0 && ((double)x.ApprovedRatingSum / (double)x.ApprovedTotalReviews) == rating);
                }
            }

            return query;
        }

        protected virtual IQueryable<Product> VisitRoleFilter(ISearchFilter filter, CatalogSearchQueryContext context, IQueryable<Product> query)
        {
            var db = context.Services.DbContext;
            if (!db.QuerySettings.IgnoreAcl)
            {
                var roleIds = filter.GetTermsArray<int>();
                if (roleIds.Length > 0)
                {
                    var entityName = nameof(Product);
                    var subQuery = db.AclRecords
                        .Where(x => x.EntityName == entityName && roleIds.Contains(x.CustomerRoleId))
                        .Select(x => x.EntityId);

                    query = query.Where(x => !x.SubjectToAcl || subQuery.Contains(x.Id));
                }
            }

            return query;
        }

        protected virtual IQueryable<Product> VisitStoreFilter(ISearchFilter filter, CatalogSearchQueryContext context, IQueryable<Product> query)
        {
            var db = context.Services.DbContext;
            if (!db.QuerySettings.IgnoreMultiStore)
            {
                var storeIds = filter.GetTermsArray<int>();
                if (storeIds.Length > 0)
                {
                    var entityName = nameof(Product);
                    var subQuery = db.StoreMappings
                        .Where(x => x.EntityName == entityName && storeIds.Contains(x.StoreId))
                        .Select(x => x.EntityId);

                    query = query.Where(x => !x.LimitedToStores || subQuery.Contains(x.Id));
                }
            }

            return query;
        }

        protected override IQueryable<Product> VisitSorting(SearchSort sorting, CatalogSearchQueryContext context, IQueryable<Product> query)
        {
            var names = CatalogSearchQuery.KnownSortings;

            if (sorting.FieldName.IsEmpty())
            {
                // Sort by relevance.
                if (context.CategoryId > 0)
                {
                    query = OrderBy(query, x => x.ProductCategories.FirstOrDefault(pc => pc.CategoryId == context.CategoryId.Value).DisplayOrder);
                }
                else if (context.ManufacturerId > 0)
                {
                    query = OrderBy(query, x => x.ProductManufacturers.FirstOrDefault(pm => pm.ManufacturerId == context.ManufacturerId.Value).DisplayOrder);
                }
            }
            else if (sorting.FieldName == names.CreatedOn)
            {
                query = OrderBy(query, x => x.CreatedOnUtc, sorting.Descending);
            }
            else if (sorting.FieldName == names.Name)
            {
                query = OrderBy(query, x => x.Name, sorting.Descending);
            }
            else if (sorting.FieldName == names.Price)
            {
                query = OrderBy(query, x => x.Price, sorting.Descending);
            }

            return query;
        }

        protected override IOrderedQueryable<Product> ApplyDefaultSorting(CatalogSearchQueryContext context, IQueryable<Product> query)
        {
            if (context.SearchQuery.Filters.FindFilter(CatalogSearchQuery.KnownFilters.ParentId) != null)
            {
                return query.OrderBy(x => x.DisplayOrder);
            }
            else if (context.CategoryId > 0 && context.SearchQuery.Origin.EqualsNoCase("Catalog/Category"))
            {
                return OrderBy(query, x => x.ProductCategories.FirstOrDefault(pc => pc.CategoryId == context.CategoryId.Value).DisplayOrder);
            }
            else if (context.ManufacturerId > 0 && context.SearchQuery.Origin.EqualsNoCase("Catalog/Manufacturer"))
            {
                return OrderBy(query, x => x.ProductManufacturers.FirstOrDefault(pm => pm.ManufacturerId == context.ManufacturerId.Value).DisplayOrder);
            }

            return query.OrderBy(x => x.Id);
        }

        private static IQueryable<Product> ApplyCategoriesFilter(IQueryable<Product> query, int[] ids, bool? featuredOnly)
        {
            return
                from p in query
                from pc in p.ProductCategories.Where(pc => ids.Contains(pc.CategoryId))
                where !featuredOnly.HasValue || featuredOnly.Value == pc.IsFeaturedProduct
                select p;
        }

        private static IQueryable<Product> ApplyManufacturersFilter(IQueryable<Product> query, int[] ids, bool? featuredOnly)
        {
            return
                from p in query
                from pm in p.ProductManufacturers.Where(pm => ids.Contains(pm.ManufacturerId))
                where !featuredOnly.HasValue || featuredOnly.Value == pm.IsFeaturedProduct
                select p;
        }
    }
}
