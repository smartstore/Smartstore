using Smartstore.Collections;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Web.Models.Catalog
{
    public partial class GroupedProductModel : EntityModelBase
    {
        public GroupedProductConfiguration Configuration { get; set; }
        public IPagedList<ProductDetailsModel> Products { get; set; }

        public bool ShowHeaderField(string name)
            => Configuration?.HeaderFields?.Any(x => x.EqualsNoCase(name)) ?? false;
    }
}
