using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Identity;
using Smartstore.Core.Stores;

namespace Smartstore.Core.DataExchange.Export
{
    /// <summary>
    /// Data exporter contract.
    /// </summary>
    public partial interface IDataExporter
    {
        /// <summary>
        /// Creates a product export context for fast retrieval (eager loading) of product navigation properties.
        /// </summary>
        /// <param name="products">Products. <c>null</c> to lazy load data if required.</param>
        /// <param name="customer">Customer. If <c>null</c>, customer will be obtained via <see cref="IWorkContext.CurrentCustomer"/>.</param>
        /// <param name="store">Store. If <c>null</c>, store will be obtained via <see cref="IStoreContext.CurrentStore"/>.</param>
        /// <param name="maxMediaPerProduct">Media files per product, <c>null</c> to load all files per product.</param>
        /// <param name="includeHidden">A value indicating whether to include hidden records.</param>
        /// <returns>Product export context</returns>
        ProductBatchContext CreateProductBatchContext(
            IEnumerable<Product> products = null,
            Customer customer = null,
            Store store = null,
            int? maxMediaPerProduct = null,
            bool includeHidden = true);
    }
}
