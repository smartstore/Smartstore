using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Search;

namespace Smartstore.Core.Catalog.Search.QueryHandlers
{
    internal class SearchTermQueryHandler : CatalogSearchQueryHandler
    {
        protected override IQueryable<Product> Apply(IQueryable<Product> query, CatalogSearchQueryContext ctx)
        {
            var term = ctx.SearchQuery.Term;
            var fields = ctx.SearchQuery.Fields;
            var languageId = ctx.SearchQuery.LanguageId ?? 0;

            if (term.HasValue() && fields != null && fields.Length != 0 && fields.Any(x => x.HasValue()))
            {
                ctx.IsGroupingRequired = true;

                var db = ctx.Services.DbContext;
                var lpQuery = db.LocalizedProperties.AsNoTracking();

                // SearchMode.ExactMatch doesn't make sense here
                if (ctx.SearchQuery.Mode == SearchMode.StartsWith)
                {
                    query =
                        from p in query
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
                    query =
                        from p in query
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

            return query;
        }
    }
}
