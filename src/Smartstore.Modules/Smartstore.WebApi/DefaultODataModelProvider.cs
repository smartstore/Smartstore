using Microsoft.OData.ModelBuilder;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Web.Api;

namespace Smartstore.WebApi
{
    internal class DefaultODataModelProvider : IODataModelProvider
    {
        public void Build(ODataModelBuilder builder)
        {
            builder.EntitySet<Category>("Categories");
            builder.EntitySet<Discount>("Discounts");
        }
    }
}
