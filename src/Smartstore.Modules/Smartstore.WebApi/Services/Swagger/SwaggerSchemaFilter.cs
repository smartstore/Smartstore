using System.ComponentModel.DataAnnotations;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Smartstore.Web.Api.Swagger
{
    internal class SwaggerSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            var mi = context.MemberInfo;
            if (mi != null && mi.TryGetAttribute<MaxLengthAttribute>(false, out var attr) && attr.Length < 0)
            {
                // Schema validation : "MaxLength" must be >= 0.
                schema.MaxLength = int.MaxValue;
            }
        }
    }
}
