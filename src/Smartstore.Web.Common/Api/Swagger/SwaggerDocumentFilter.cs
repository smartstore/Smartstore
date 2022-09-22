using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using OpenApiOpType = Microsoft.OpenApi.Models.OperationType;

namespace Smartstore.Web.Api.Swagger
{
    /// <summary>
    /// Filters OData paths that should not be displayed in the Swagger documentation.
    /// </summary>
    public class SwaggerDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            // INFO: it would be nice if we could strip off the OData route here: /odata/v1/categories({key}) -> /categories({key})
            // but that would let Swagger execute against /categories({key}) always resulting in 404 NotFound.
            // To achieve this a custom Swagger template would be required. Perhaps there is an extension somewhere.

            foreach (var path in swaggerDoc.Paths)
            {
                //$"{path.Key} {string.Join(",", path.Value.Operations.Select(x => x.Key))}".Dump();

                // Skip duplicate documents. No need to have both, GET "entityset({key})" and GET "entityset/{key}".
                // Skip unusual, unexpected documents like $count.
                var removePath = (path.Key.EndsWithNoCase("/{key}") || path.Key.EndsWithNoCase("/$count"))
                    && path.Value.Operations.Any(x => x.Key == OpenApiOpType.Get);

                if (removePath)
                {
                    swaggerDoc.Paths.Remove(path.Key);
                }
            }
        }
    }
}
