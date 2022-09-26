using Microsoft.OData.ModelBuilder;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;

namespace Smartstore.Web.Api
{
    internal class DefaultODataModelProvider : IODataModelProvider
    {
        public void Build(ODataModelBuilder builder, int version)
        {
            builder.EntitySet<Category>("Categories");
            builder.EntitySet<Discount>("Discounts");
        }
    }
}
