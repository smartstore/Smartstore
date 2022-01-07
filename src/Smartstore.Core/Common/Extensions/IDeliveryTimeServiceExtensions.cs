using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;

namespace Smartstore
{
    public static class IDeliveryTimeServiceExtensions
    {
        /// <summary>
        /// Gets the product delivery time according to stock.
        /// </summary>
        public static Task<DeliveryTime> GetDeliveryTimeAsync(this IDeliveryTimeService service, Product product, CatalogSettings catalogSettings)
        {
            var deliveryTimeId = product.GetDeliveryTimeIdAccordingToStock(catalogSettings);
            return service.GetDeliveryTimeAsync(deliveryTimeId, false);
        }
    }
}
