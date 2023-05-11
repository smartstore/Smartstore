using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Products;
using Smartstore.Web.Models.Catalog;

namespace Smartstore.DevTools
{
    public class MapperCallbackTest : IMapperCallback<Product, ProductDetailsModel>
    {
        public async Task MapCallbackAsync(Product from, ProductDetailsModel to, dynamic parameters = null)
        {
            to.CustomProperties["TestId"] = from.Id;
        }
    }
}