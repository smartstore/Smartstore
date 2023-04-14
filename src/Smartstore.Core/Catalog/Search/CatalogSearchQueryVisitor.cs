using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Search;

namespace Smartstore.Core.Catalog.Search
{
    public class CatalogSearchQueryVisitor
    {
        public CatalogSearchQueryVisitor(IQueryable<Product> dbQuery)
        {
            DbQuery = Guard.NotNull(dbQuery);
        }

        public CatalogSearchQuery SearchQuery 
        {
            get => Context.SearchQuery;
        }

        public IQueryable<Product> DbQuery { get; protected set; }
        public CatalogSearchQueryContext Context { get; private set; }

        public virtual void Visit(CatalogSearchQueryContext context)
        {
            Context = Guard.NotNull(context);

            // TODO: (mg) Refactor after Terms isolation is implemented.
            VisitTerm();

            for (var i = 0; i < context.Filters.Count; i++)
            {
                var filter = context.Filters[i];

                if (filter is INamedSearchFilter namedFilter)
                {
                    VisitNamedFilter(namedFilter);
                }
                else
                {
                    VisitFilter(filter);
                }
            }
        }

        protected virtual void VisitTerm()
        {
            // TODO: (mg) Refactor after Terms isolation is implemented.
            var term = Context.SearchQuery.Term;
            var fields = Context.SearchQuery.Fields;
            var languageId = Context.SearchQuery.LanguageId ?? 0;

            if (term.HasValue() && fields != null && fields.Length != 0 && fields.Any(x => x.HasValue()))
            {
                Context.IsGroupingRequired = true;

                var lpQuery = Context.Services.DbContext.LocalizedProperties.AsNoTracking();

                // SearchMode.ExactMatch doesn't make sense here
                if (Context.SearchQuery.Mode == SearchMode.StartsWith)
                {
                    DbQuery =
                        from p in DbQuery
                        join lp in lpQuery on p.Id equals lp.EntityId into plp
                        from lp in plp.DefaultIfEmpty()
                        where
                            (fields.Contains("name") && p.Name.StartsWith(term)) ||
                            (fields.Contains("sku") && p.Sku.StartsWith(term)) ||
                            (fields.Contains("shortdescription") && p.ShortDescription.StartsWith(term)) ||
                            (languageId != 0 && lp.LanguageId == languageId && lp.LocaleKeyGroup == "Product" && lp.LocaleKey == "Name" && lp.LocaleValue.StartsWith(term)) ||
                            (languageId != 0 && lp.LanguageId == languageId && lp.LocaleKeyGroup == "Product" && lp.LocaleKey == "ShortDescription" && lp.LocaleValue.StartsWith(term))
                        select p;
                }
                else
                {
                    DbQuery =
                        from p in DbQuery
                        join lp in lpQuery on p.Id equals lp.EntityId into plp
                        from lp in plp.DefaultIfEmpty()
                        where
                            (fields.Contains("name") && p.Name.Contains(term)) ||
                            (fields.Contains("sku") && p.Sku.Contains(term)) ||
                            (fields.Contains("shortdescription") && p.ShortDescription.Contains(term)) ||
                            (languageId != 0 && lp.LanguageId == languageId && lp.LocaleKeyGroup == "Product" && lp.LocaleKey == "Name" && lp.LocaleValue.Contains(term)) ||
                            (languageId != 0 && lp.LanguageId == languageId && lp.LocaleKeyGroup == "Product" && lp.LocaleKey == "ShortDescription" && lp.LocaleValue.Contains(term))
                        select p;
                }
            }
        }

        protected virtual void VisitFilter(ISearchFilter filter)
        {
        }

        protected virtual void VisitNamedFilter(INamedSearchFilter filter)
        {
            if (filter.FieldName == "id")
            {
                ApplyMemberExpression(x => x.Id, filter);

                //if (filter is IRangeSearchFilter rf)
                //{
                //    var lower = rf.Term as int?;
                //    var upper = rf.UpperTerm as int?;

                //    if (lower.HasValue)
                //    {
                //        if (rf.IncludesLower)
                //            DbQuery = DbQuery.Where(x => x.Id >= lower.Value);
                //        else
                //            DbQuery = DbQuery.Where(x => x.Id > lower.Value);
                //    }

                //    if (upper.HasValue)
                //    {
                //        if (rf.IncludesUpper)
                //            DbQuery = DbQuery.Where(x => x.Id <= upper.Value);
                //        else
                //            DbQuery = DbQuery.Where(x => x.Id < upper.Value);
                //    }
                //}
                //else
                //{
                //    var productIds = filter.GetTermsArray<int>();
                //    if (productIds.Length == 1)
                //    {
                //        DbQuery = DbQuery.Where(x => x.Id == productIds[0]);
                //    }
                //    else if (productIds.Length > 1)
                //    {
                //        DbQuery = DbQuery.Where(x => productIds.Contains(x.Id));
                //    }
                //}
            }
            else if (filter.FieldName == "stockquantity")
            {
                ApplyMemberExpression(x => x.StockQuantity, filter);
            }
            else if (filter.FieldName == "deliveryid")
            {
                ApplyMemberExpression(x => x.DeliveryTimeId, filter);
            }
            else if (filter.FieldName == "parentid")
            {
                ApplyMemberExpression(x => x.ParentGroupedProductId, filter);
            }
            else if (filter.FieldName == "categoryid")
            {
                if (filter is IRangeSearchFilter rf)
                {
                    // Has any category.
                    if (1 == ((rf.Term as int?) ?? 0) && int.MaxValue == ((rf.UpperTerm as int?) ?? 0))
                    {
                        DbQuery = DbQuery.Where(x => x.ProductCategories.Count > 0);
                    }
                }
                else
                {
                    var categoryIds = filter.GetTermsArray<int>();
                    if (categoryIds.Length > 0)
                    {
                        Context.CategoryId ??= categoryIds.First();
                        if (categoryIds.Length == 1 && Context.CategoryId == 0)
                        {
                            // Has no category.
                            DbQuery = DbQuery.Where(x => x.ProductCategories.Count == 0);
                        }
                        else
                        {
                            Context.IsGroupingRequired = true;
                            DbQuery = ApplyCategoriesFilter(DbQuery, categoryIds, null);
                        }
                    }
                }
            }
            else if (filter.FieldName == "featuredcategoryid")
            {
                var featuredCategoryIds = filter.GetTermsArray<int>();
                if (featuredCategoryIds.Length > 0)
                {
                    Context.IsGroupingRequired = true;
                    Context.CategoryId ??= featuredCategoryIds.First();
                    DbQuery = ApplyCategoriesFilter(DbQuery, featuredCategoryIds, true);
                }
            }
            else if (filter.FieldName == "notfeaturedcategoryid")
            {
                var notFeaturedCategoryIds = filter.GetTermsArray<int>();
                if (notFeaturedCategoryIds.Length > 0)
                {
                    Context.IsGroupingRequired = true;
                    Context.CategoryId ??= notFeaturedCategoryIds.First();
                    DbQuery = ApplyCategoriesFilter(DbQuery, notFeaturedCategoryIds, false);
                }
            }
            else if (filter.FieldName == "manufacturerid")
            {
                if (filter is IRangeSearchFilter rf)
                {
                    // Has any manufacturer.
                    if (1 == ((rf.Term as int?) ?? 0) && int.MaxValue == ((rf.UpperTerm as int?) ?? 0))
                    {
                        DbQuery = DbQuery.Where(x => x.ProductManufacturers.Count > 0);
                    }
                }
                else
                {
                    var manufacturerIds = filter.GetTermsArray<int>();
                    if (manufacturerIds.Length > 0)
                    {
                        Context.ManufacturerId ??= manufacturerIds.First();
                        if (manufacturerIds.Length == 1 && Context.ManufacturerId == 0)
                        {
                            // Has no manufacturer.
                            DbQuery = DbQuery.Where(x => x.ProductManufacturers.Count == 0);
                        }
                        else
                        {
                            Context.IsGroupingRequired = true;
                            DbQuery = ApplyManufacturersFilter(DbQuery, manufacturerIds, null);
                        }
                    }
                }
            }
            else if (filter.FieldName == "featuredmanufacturerid")
            {
                var featuredManuIds = filter.GetTermsArray<int>();
                if (featuredManuIds.Length > 0)
                {
                    Context.IsGroupingRequired = true;
                    Context.ManufacturerId ??= featuredManuIds.First();
                    DbQuery = ApplyManufacturersFilter(DbQuery, featuredManuIds, true);
                }
            }
            else if (filter.FieldName == "notfeaturedmanufacturerid")
            {
                var notFeaturedManuIds = filter.GetTermsArray<int>();
                if (notFeaturedManuIds.Length > 0)
                {
                    Context.IsGroupingRequired = true;
                    Context.ManufacturerId ??= notFeaturedManuIds.First();
                    DbQuery = ApplyManufacturersFilter(DbQuery, notFeaturedManuIds, false);
                }
            }
            else if (filter.FieldName == "tagid")
            {
                var tagIds = filter.GetTermsArray<int>();
                if (tagIds.Length > 0)
                {
                    Context.IsGroupingRequired = true;
                    DbQuery =
                        from p in DbQuery
                        from pt in p.ProductTags.Where(pt => tagIds.Contains(pt.Id))
                        select p;
                }
            }
            else if (filter.FieldName == "condition")
            {
                var conditions = filter.GetTermsArray<int>();
                if (conditions.Length == 1)
                {
                    DbQuery = DbQuery.Where(x => (int)x.Condition == conditions[0]);
                }
                else if (conditions.Length > 1)
                {
                    DbQuery = DbQuery.Where(x => conditions.Contains((int)x.Condition));
                }
            }
        }

        protected virtual void VisitRangeFilter(IRangeSearchFilter filter)
        {
            if (filter.FieldName == "availablestart")
            {
                var lower = filter.Term as DateTime?;
                var upper = filter.UpperTerm as DateTime?;

                if (lower.HasValue)
                {
                    if (filter.IncludesLower)
                        DbQuery = DbQuery.Where(x => !x.AvailableStartDateTimeUtc.HasValue || x.AvailableStartDateTimeUtc >= lower.Value);
                    else
                        DbQuery = DbQuery.Where(x => !x.AvailableStartDateTimeUtc.HasValue || x.AvailableStartDateTimeUtc > lower.Value);
                }

                if (upper.HasValue)
                {
                    if (filter.IncludesLower)
                        DbQuery = DbQuery.Where(x => !x.AvailableStartDateTimeUtc.HasValue || x.AvailableStartDateTimeUtc <= upper.Value);
                    else
                        DbQuery = DbQuery.Where(x => !x.AvailableStartDateTimeUtc.HasValue || x.AvailableStartDateTimeUtc < upper.Value);
                }
            }
            else if (filter.FieldName == "availableend")
            {
                var lower = filter.Term as DateTime?;
                var upper = filter.UpperTerm as DateTime?;

                if (lower.HasValue)
                {
                    if (filter.IncludesLower)
                        DbQuery = DbQuery.Where(x => !x.AvailableEndDateTimeUtc.HasValue || x.AvailableEndDateTimeUtc >= lower.Value);
                    else
                        DbQuery = DbQuery.Where(x => !x.AvailableEndDateTimeUtc.HasValue || x.AvailableEndDateTimeUtc > lower.Value);
                }

                if (upper.HasValue)
                {
                    if (filter.IncludesLower)
                        DbQuery = DbQuery.Where(x => !x.AvailableEndDateTimeUtc.HasValue || x.AvailableEndDateTimeUtc <= upper.Value);
                    else
                        DbQuery = DbQuery.Where(x => !x.AvailableEndDateTimeUtc.HasValue || x.AvailableEndDateTimeUtc < upper.Value);
                }
            }
            else if (filter.FieldName == "rating")
            {
                var lower = filter.Term as double?;
                var upper = filter.UpperTerm as double?;

                if (lower.HasValue)
                {
                    if (filter.IncludesLower)
                        DbQuery = DbQuery.Where(x => x.ApprovedTotalReviews > 0 && ((double)x.ApprovedRatingSum / (double)x.ApprovedTotalReviews) >= lower.Value);
                    else
                        DbQuery = DbQuery.Where(x => x.ApprovedTotalReviews > 0 && ((double)x.ApprovedRatingSum / (double)x.ApprovedTotalReviews) > lower.Value);
                }

                if (upper.HasValue)
                {
                    if (filter.IncludesUpper)
                        DbQuery = DbQuery.Where(x => x.ApprovedTotalReviews > 0 && ((double)x.ApprovedRatingSum / (double)x.ApprovedTotalReviews) <= upper.Value);
                    else
                        DbQuery = DbQuery.Where(x => x.ApprovedTotalReviews > 0 && ((double)x.ApprovedRatingSum / (double)x.ApprovedTotalReviews) < upper.Value);
                }
            }
            else if (filter.FieldName == "createdon")
            {
                var lower = filter.Term as DateTime?;
                var upper = filter.UpperTerm as DateTime?;

                if (lower.HasValue)
                {
                    if (filter.IncludesLower)
                        DbQuery = DbQuery.Where(x => x.CreatedOnUtc >= lower.Value);
                    else
                        DbQuery = DbQuery.Where(x => x.CreatedOnUtc > lower.Value);
                }

                if (upper.HasValue)
                {
                    if (filter.IncludesLower)
                        DbQuery = DbQuery.Where(x => x.CreatedOnUtc <= upper.Value);
                    else
                        DbQuery = DbQuery.Where(x => x.CreatedOnUtc < upper.Value);
                }
            }
            else if (filter.FieldName.StartsWith("price"))
            {
                var lower = filter.Term as double?;
                var upper = filter.UpperTerm as double?;

                if (lower.HasValue)
                {
                    var minPrice = Convert.ToDecimal(lower.Value);

                    DbQuery = DbQuery.Where(x =>
                        ((x.SpecialPrice.HasValue &&
                        ((!x.SpecialPriceStartDateTimeUtc.HasValue || x.SpecialPriceStartDateTimeUtc.Value < Context.Now) &&
                        (!x.SpecialPriceEndDateTimeUtc.HasValue || x.SpecialPriceEndDateTimeUtc.Value > Context.Now))) &&
                        (x.SpecialPrice >= minPrice))
                        ||
                        ((!x.SpecialPrice.HasValue ||
                        ((x.SpecialPriceStartDateTimeUtc.HasValue && x.SpecialPriceStartDateTimeUtc.Value > Context.Now) ||
                        (x.SpecialPriceEndDateTimeUtc.HasValue && x.SpecialPriceEndDateTimeUtc.Value < Context.Now))) &&
                        (x.Price >= minPrice))
                    );
                }

                if (upper.HasValue)
                {
                    var maxPrice = Convert.ToDecimal(upper);

                    DbQuery = DbQuery.Where(x =>
                        ((x.SpecialPrice.HasValue &&
                        ((!x.SpecialPriceStartDateTimeUtc.HasValue || x.SpecialPriceStartDateTimeUtc.Value < Context.Now) &&
                        (!x.SpecialPriceEndDateTimeUtc.HasValue || x.SpecialPriceEndDateTimeUtc.Value > Context.Now))) &&
                        (x.SpecialPrice <= maxPrice))
                        ||
                        ((!x.SpecialPrice.HasValue ||
                        ((x.SpecialPriceStartDateTimeUtc.HasValue && x.SpecialPriceStartDateTimeUtc.Value > Context.Now) ||
                        (x.SpecialPriceEndDateTimeUtc.HasValue && x.SpecialPriceEndDateTimeUtc.Value < Context.Now))) &&
                        (x.Price <= maxPrice))
                    );
                }
            }
        }

        protected void ApplyMemberExpression<TMember>(
            Expression<Func<Product, TMember>> memberExpression, 
            INamedSearchFilter filter)
        {
            //var descriptor = new FilterDescriptor<Product, TMember>(memberExpression);
            //var op = filter.Occurence == SearchFilterOccurence.MustNot ? RuleOperator.IsNotEqualTo : RuleOperator.IsEqualTo;
            //var terms = Array.Empty<TMember>();
            //var isRange = false;

            //if (filter is IAttributeSearchFilter attrFilter)
            //{
            //    terms = attrFilter.GetTermsArray<TMember>();
            //}

            //if (filter is IRangeSearchFilter rangeFilter)
            //{
            //    isRange = true;
            //    var upper = rangeFilter.UpperTerm.Convert<TMember>();
            //}

            //var filterExpression = new FilterExpression
            //{
            //    Descriptor = descriptor,
            //    Operator = op,
            //    LogicalOperator = LogicalRuleOperator.And,
            //    Value = terms[0]
            //};

            //DbQuery = DbQuery.Where(filterExpression).Cast<Product>();
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
