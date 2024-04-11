using Smartstore.Collections;

namespace Smartstore.Web.Models.Catalog
{
    public partial class AssociatedProductsListModel : ModelBase
    {
        public IPagedList<ProductDetailsModel> Products { get; set; }
    }
}
