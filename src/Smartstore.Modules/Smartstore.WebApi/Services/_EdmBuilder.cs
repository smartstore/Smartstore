using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;

namespace Smartstore.WebApi.Services
{
    // TODO: (mg) (core) EDM builder cannot access and include entities of modules like BlogPost.
    // TODO: (mg) (core) in real scenario this should have a provider (or provide by IWebApiService):
    //public interface IODataModelProvider
    //{
    //    IEdmModel GetEdmModel(string apiVersion);
    //}
    internal static class EdmBuilder
    {
        public static IEdmModel BuildV1Model()
        {
            var b = new ODataConventionModelBuilder();

            b.EntitySet<Category>("Categories");
            b.EntitySet<Discount>("Discounts");

            return b.GetEdmModel();
        }
    }
}
