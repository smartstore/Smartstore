using Smartstore.Collections;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Web.Models.Catalog
{
    public partial class GroupedProductModel : ModelBase
    {
        public GroupedProductConfiguration Configuration { get; set; }
        public IPagedList<ProductDetailsModel> Products { get; set; }
    }
}
