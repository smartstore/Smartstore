using Autofac;
using Smartstore.Core;
using Smartstore.Core.Common;
using Smartstore.Engine;
using Smartstore.Core.Domain.Catalog;
using System.Linq;

namespace Smartstore
{
    public static class QuantityUnitQueryExtensions
    {
        /// <summary>
        /// Applies filter by <see cref="QuantityUnit.Id"/> and orders by <see cref="QuantityUnit.DisplayOrder"/>
        /// </summary>
        /// <returns>
        /// IQueryable with quantity unit by id or default quantity unit corresponding to <see cref="CatalogSettings.ShowDefaultQuantityUnit"/>
        /// </returns>
        public static IQueryable<QuantityUnit> ApplyQuantityUnitFilter(this IQueryable<QuantityUnit> query, int? quantityUnitId)
        {
            Guard.NotNull(query, nameof(query));

            if (quantityUnitId == 0 || quantityUnitId == null)
            {
                var catalogSettings = EngineContext.Current.Application.Services.Resolve<CatalogSettings>();

                if (catalogSettings.ShowDefaultQuantityUnit)
                {
                    return query.Where(x => x.IsDefault);
                }

                return null;
            }

            query = from x in query
                    where x.Id == quantityUnitId
                    orderby x.DisplayOrder
                    select x;

            return query;
        }
    }
}
