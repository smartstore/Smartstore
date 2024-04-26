//using Microsoft.OpenApi.Any;
//using Microsoft.OpenApi.Models;
//using Smartstore.Domain;
//using Swashbuckle.AspNetCore.SwaggerGen;

//namespace Smartstore.Web.Api.Swagger
//{
//    //Schema filtering does not work as expected if you only want to reduce the depth of the generated examples
//    //without changing the actual schema definition.See https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/615
//    // Data type see: https://swagger.io/docs/specification/data-models/data-types/

//    public class SwaggerSchemaFilter : ISchemaFilter
//    {
//        //private const int PropertyDepth = 1;
//        private static readonly Type BaseEntityType = typeof(BaseEntity);

//        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
//        {
//            //if (context.Type.IsEnum)
//            //{
//            //    var array = new OpenApiArray();
//            //    array.AddRange(Enum.GetNames(context.Type).Select(n => new OpenApiString(n)));
//            //    // NSwag
//            //    schema.Extensions.Add("x-enumNames", array);
//            //    // Openapi-generator
//            //    schema.Extensions.Add("x-enum-varnames", array);
//            //}



//            //if (!context.SchemaRepository.TryLookupByType(typeof(EmptyOpenApiObject), out _))
//            //{
//            //    context.SchemaRepository.RegisterType(typeof(EmptyOpenApiObject), nameof(EmptyOpenApiObject));
//            //}

//            // Recursion does not work. The property schema never has properties :( Only the root schema provided by "Apply" has properties.
//            //RecursionTest(schema, context.Type, 0);

//            //if (context.Type == typeof(Category))
//            //if (BaseEntityType.IsAssignableFrom(context.Type))
//            //{
//            //    var properties = FastProperty.GetProperties(context.Type).Values.Where(x => x.IsComplexType);

//            //    foreach (var prop in properties)
//            //    {
//            //        var type = prop.Property.PropertyType;
//            //        var isEntityRef = BaseEntityType.IsAssignableFrom(type);
//            //        var isCollection = !isEntityRef && type.IsClosedGenericTypeOf(typeof(ICollection<>)) && BaseEntityType.IsAssignableFrom(type.GenericTypeArguments[0]);

//            //        if ((isEntityRef || isCollection) && schema.Properties.TryGetValue(prop.Name, out var propSchema))
//            //        {
//            //            ClearPropertyExample(propSchema, isCollection);
//            //        }
//            //    }
//            //}
//        }

//        //private static void ClearPropertyExample(OpenApiSchema schema, bool isCollection)
//        //{
//        //    if (schema != null)
//        //    {
//        //        if (isCollection)
//        //        {
//        //            schema.Type = "array";
//        //            schema.Items = new()
//        //            {
//        //                Type = "object",
//        //                Example = new OpenApiObject()
//        //            };
//        //        }
//        //        else
//        //        {
//        //            schema.Type = "object";
//        //            schema.Example = new OpenApiObject();
//        //            schema.Reference = null;
//        //            //schema.UnresolvedReference = true;
//        //        }
//        //    }

//        //    //if (schema.Properties.ContainsKey(propertyName))
//        //    //{
//        //    //    schema.Properties.Remove(propertyName);
//        //    //    schema.Properties.Add(propertyName, isCollection ? EmptyArraySchema : EmptyObjectSchema);
//        //    //}
//        //}

//        //private void RecursionTest(OpenApiSchema schema, Type entityType, int depth)
//        //{
//        //    if (entityType != null && BaseEntityType.IsAssignableFrom(entityType))
//        //    {
//        //        var properties = FastProperty.GetProperties(entityType).Values.Where(x => x.IsComplexType);

//        //        foreach (var prop in properties)
//        //        {
//        //            var type = prop.Property.PropertyType;
//        //            var isEntityRef = BaseEntityType.IsAssignableFrom(type);
//        //            var isCollection = !isEntityRef && type.IsClosedGenericTypeOf(typeof(ICollection<>)) && BaseEntityType.IsAssignableFrom(type.GenericTypeArguments[0]);

//        //            $"{depth}: {schema.Properties.ContainsKey(prop.Name)} {prop.Name}".Dump();

//        //            if ((isEntityRef || isCollection) && schema.Properties.TryGetValue(prop.Name, out var propSchema))
//        //            {
//        //                // propSchema has no properties here :(
//        //                if (depth < PropertyDepth)
//        //                {
//        //                    // Go deeper.
//        //                    RecursionTest(propSchema,
//        //                        isEntityRef ? type : type.GenericTypeArguments[0],
//        //                        depth + 1);
//        //                }
//        //                else
//        //                {
//        //                    // Get out at this depth.
//        //                    ClearPropertyExample(propSchema, isCollection);
//        //                }
//        //            }
//        //        }
//        //    }
//        //}
//    }
//}
