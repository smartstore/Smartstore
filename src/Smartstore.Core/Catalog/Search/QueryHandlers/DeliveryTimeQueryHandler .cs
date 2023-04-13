using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Catalog.Search.QueryHandlers
{
    internal class DeliveryTimeQueryHandler : CatalogSearchQueryHandler
    {
        protected override IQueryable<Product> Apply(IQueryable<Product> query, CatalogSearchQueryContext ctx)
        {
            var deliverTimeIds = GetIdList("deliveryid", ctx);
            if (deliverTimeIds.Length > 0)
            {
                query = query.Where(x => x.DeliveryTimeId != null && deliverTimeIds.Contains(x.DeliveryTimeId.Value));
            }

            return query;
        }
    }
}
