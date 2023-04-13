using System.Runtime.CompilerServices;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Search;

namespace Smartstore.Core.Catalog.Search
{
    /*
        Ordering:
        ===========
        SearchTermQueryHandler
        ProductIdQueryHandler
        CategoryIdQueryHandler
        ManufacturerIdQueryHandler
        TagQueryHandler
        DeliveryTimeQueryHandler
        ParentProductQueryHandler
        ConditionQueryHandler
        FilterQueryHandler
    */

    public abstract class CatalogSearchQueryHandler : SearchQueryHandler<Product, CatalogSearchQuery>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed IQueryable<Product> Apply(IQueryable<Product> query, SearchQueryContext<CatalogSearchQuery> ctx)
            => Apply(query, (CatalogSearchQueryContext)ctx);

        protected abstract IQueryable<Product> Apply(IQueryable<Product> query, CatalogSearchQueryContext ctx);
    }
}
