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
            foreach (var path in swaggerDoc.Paths)
            {
                foreach (var op in path.Value.Operations.Keys)
                {
                    //$"{op} {path.Key}".Dump();

                    switch (op)
                    {
                        case OpenApiOpType.Get:
                            // Remove duplicate documents. No need to have both, GET "entityset({key})" and GET "entityset/{key}".
                            // Remove unusual, unexpected documents like $count.
                            if (path.Key.EndsWithNoCase("/{key}") || path.Key.EndsWithNoCase("/$count"))
                            {
                                swaggerDoc.Paths.Remove(path.Key);
                            }
                            break;
                    }
                }
            }
        }
    }
}
