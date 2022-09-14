using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;

namespace Smartstore.WebApi.Services
{
    // RE RE: No. MapODataRoute is gone in ASP.NET.OData 8. https://devblogs.microsoft.com/odata/asp-net-odata-8-0-preview-for-net-5/#register-the-odata-services
    // It looks like we have to provide our own infrastructure that is able to absorb such breaking changes in ASP.NET.OData.
    // There can be only one AddRouteComponents-call per routePrefix (thus API version like odata/v1) otherwise InvalidOperationException.
    // Means we need a kind of collecting mechanism with the goal of creating a final IEdmModel used in an exclusive AddRouteComponents-call.

    //internal static class EdmBuilder
    //{
    //    public static IEdmModel BuildV1Model()
    //    {
    //        var b = new ODataConventionModelBuilder();

    //        b.EntitySet<Category>("Categories");
    //        b.EntitySet<Discount>("Discounts");

    //        return b.GetEdmModel();
    //    }
    //}
}
