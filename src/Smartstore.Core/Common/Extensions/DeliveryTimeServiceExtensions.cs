using System.Threading.Tasks;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Domain.Catalog;

namespace Smartstore
{
    public static class DeliveryTimeServiceExtensions
    {
        /// <summary>
        /// Gets the product delivery time according to stock.
        /// </summary>
        public static Task<DeliveryTime> GetDeliveryTimeAsync(this IDeliveryTimeService service, Product product, CatalogSettings catalogSettings)
        {
            var deliveryTimeId = product.GetDeliveryTimeIdAccordingToStock(catalogSettings);
            return service.GetDeliveryTimeAsync(deliveryTimeId, true);
        }
    }
}
