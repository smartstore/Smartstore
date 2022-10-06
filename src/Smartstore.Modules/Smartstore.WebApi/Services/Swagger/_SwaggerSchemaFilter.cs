using System.Reflection;
using System.Xml.Linq;
using Microsoft.OpenApi.Models;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Domain;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Smartstore.Web.Api.Swagger
{
    internal class SwaggerSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            // Is it worth all that hackish code?

            //if (context.Type == typeof(Category))
            if (typeof(BaseEntity).IsAssignableFrom(context.Type))
            {
                var properties = FastProperty.GetProperties(context.Type).Values;

                foreach (var prop in properties)
                {
                    var type = prop.Property.PropertyType;
                    var t1 = typeof(BaseEntity).IsAssignableFrom(type);
                    var t2 = type.IsClosedGenericTypeOf(typeof(ICollection<>));
                    var t3 = t2 && typeof(BaseEntity).IsAssignableFrom(type.GenericTypeArguments[0]);

                    //$"{prop.Name} {t1} {t2} {t3}".Dump();

                    if ((t1 || t3) && schema.Properties.TryGetValue(prop.Name, out var schemaProp))
                    {
                        schema.Properties.Remove(prop.Name);
                        schema.Properties.Add(prop.Name, new OpenApiSchema
                        {
                            Type = "..."    // becomes "Unknown Type: ..."
                        });
                    }
                }
            }
        }
    }
}
