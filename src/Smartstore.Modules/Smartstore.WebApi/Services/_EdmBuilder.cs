using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Smartstore.Core.Catalog.Categories;

namespace Smartstore.WebApi.Services
{
    // TODO: (mg) (core) EDM builder cannot access and include entities of modules like BlogPost.
    internal static class EdmBuilder
    {
        public static IEdmModel BuildV1Model()
        {
            var b = new ODataConventionModelBuilder();

            b.EntitySet<Category>("Categories");

            return b.GetEdmModel();
        }
    }
}
