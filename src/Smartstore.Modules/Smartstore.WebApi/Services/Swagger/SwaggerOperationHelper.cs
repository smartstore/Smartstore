using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Smartstore.ComponentModel;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Smartstore.Web.Api.Swagger
{
    internal class SwaggerOperationHelper
    {
        public SwaggerOperationHelper(OpenApiOperation operation, OperationFilterContext context)
        {
            Op = operation;
            Context = context;
        }

        public OpenApiOperation Op { get; }
        public OperationFilterContext Context { get; }

        public string ActionName
            => Context.MethodInfo.Name.EmptyNull();

        public Type EntityType
            => Context.MethodInfo.DeclaringType.BaseType.GenericTypeArguments[0];

        public OpenApiSchema GenerateSchema(Type modelType)
            => Context.SchemaGenerator.GenerateSchema(modelType, Context.SchemaRepository);

        public IEnumerable<FastProperty> GetEntityProperties(Func<FastProperty, bool> predicate = null)
        {
            var properties = FastProperty.GetProperties(EntityType).Values
                .Where(x => !x.Property.HasAttribute<NotMappedAttribute>(false) && !x.Property.HasAttribute<IgnoreDataMemberAttribute>(false));

            return predicate != null ? properties.Where(predicate) : properties;
        }

        public OpenApiRequestBody CreateRequestBody()
        {
            var required = true;
            string description = null;

            switch (ActionName)
            {
                case "Post":
                    description = $"All required fields must be provided in order to successfully create a new {EntityType.Name}.";
                    break;
                case "Put":
                    description = $"All required fields must be provided in order to successfully update a {EntityType.Name}.";
                    break;
                case "Patch":
                    required = false;
                    description = "Provide those fields that should be updated.";
                    break;
            }

            return new OpenApiRequestBody
            {
                Description = "The request body. " + description,
                Required = required,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    [MediaTypeNames.Application.Json] = new OpenApiMediaType
                    {
                        Schema = GenerateSchema(EntityType)
                    }
                }
            };
        }

        public OpenApiResponse CreateSucccessResponse(bool isSingleResult)
        {
            var entityType = EntityType;
            var modelType = isSingleResult ? entityType : typeof(IQueryable<>).MakeGenericType(entityType);

            var returnStr = isSingleResult
                ? $"Returns details of {entityType.Name}."
                : $"Returns a list of {entityType.Name}.";

            if (ActionName == "Put" || ActionName == "Patch")
            {
                returnStr += " This response is only returned if the HTTP header **Prefer** with the value **return=representation** is sent.";
            }

            return new OpenApiResponse
            {
                Description = $"The request has succeeded. {returnStr}",
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    [MediaTypeNames.Application.Json] = new OpenApiMediaType
                    {
                        Schema = GenerateSchema(modelType)
                    }
                }
            };
        }

        public OpenApiResponse CreateSucccessResponse(Type modelType)
        {
            var returnStr = modelType == null
                ? "Returns a value of varying type."
                : $"Returns a value of type {modelType.Name}";

            // "string" is fine because it is just an example value.
            modelType ??= typeof(string);

            return new OpenApiResponse
            {
                Description = $"The request has succeeded. {returnStr}",
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    [MediaTypeNames.Application.Json] = new OpenApiMediaType
                    {
                        Schema = GenerateSchema(modelType)
                    }
                }
            };
        }

        public void AddKeyParameter()
        {
            var parameter = Op.Parameters.FirstOrDefault(x => x.Name == "key");
            var addParameter = parameter == null;

            parameter ??= new OpenApiParameter
            {
                Name = "key",
                Required = true,
                In = ParameterLocation.Path,
                Schema = GenerateSchema(typeof(int))
            };

            parameter.Description ??= $"The identifier of the {EntityType.Name}.";
            parameter.Example ??= new OpenApiInteger(12345);

            if (addParameter)
            {
                Op.Parameters.Add(parameter);
            }
        }

        public void AddPropertyParameter()
        {
            var parameter = Op.Parameters.FirstOrDefault(x => x.Name == "property");
            var addParameter = parameter == null;

            parameter ??= new OpenApiParameter
            {
                Name = "property",
                Required = true,
                In = ParameterLocation.Path,
                Schema = GenerateSchema(typeof(string))
            };

            parameter.Description ??= "The name of the property whose value is to be returned.";

            if (parameter.Example == null)
            {
                var prop = GetEntityProperties(x => x.Property.PropertyType.IsBasicType()).FirstOrDefault();
                if (prop != null)
                {
                    parameter.Example = new OpenApiString(prop.Property.Name);
                }
            }

            if (addParameter)
            {
                Op.Parameters.Add(parameter);
            }
        }

        public string BuildQueryExample(AllowedQueryOptions option)
        {
            string example = null;
            FastProperty prop;

            switch (option)
            {
                case AllowedQueryOptions.Filter:
                    prop = GetEntityProperties(x => x.Property.PropertyType == typeof(string) || x.Property.PropertyType == typeof(int)).FirstOrDefault();
                    if (prop != null)
                    {
                        example = prop.Property.PropertyType == typeof(string)
                            ? $"{prop.Name} eq 'iPhone Plus'"
                            : $"{prop.Name} eq 123";
                    }

                    example = "$filter=" + (example ?? "Name eq 'iPhone Plus'");
                    break;
                case AllowedQueryOptions.Expand:
                    prop = GetEntityProperties(x => x.Property.PropertyType.IsClosedGenericTypeOf(typeof(ICollection<>))).FirstOrDefault();
                    example = "$expand=" + (prop?.Property?.Name ?? "TierPrices");
                    break;
                case AllowedQueryOptions.Select:
                    prop = GetEntityProperties(x => x.Property.PropertyType.IsBasicType()).FirstOrDefault();
                    example = "$select=" + (prop?.Property?.Name ?? "Name");
                    break;
                case AllowedQueryOptions.OrderBy:
                    prop = GetEntityProperties(x => x.Property.PropertyType.IsBasicType()).FirstOrDefault();
                    example = "$orderby=" + (prop?.Property?.Name ?? "Name") + " desc";
                    break;
                case AllowedQueryOptions.Top:
                    example = "$top=50";
                    break;
                case AllowedQueryOptions.Skip:
                    example = "$skip=200";
                    break;
                case AllowedQueryOptions.Count:
                    example = "$count=true";
                    break;
                case AllowedQueryOptions.Compute:
                    example = "$compute=Price mul OrderMinimumQuantity as MinSpentPrice&$select=MinSpentPrice";
                    break;
                case AllowedQueryOptions.Search:
                    example = "$search=blue OR green";
                    break;
                default:
                    return null;
            }

            return $"Example: **{example}**.";
        }
    }
}
