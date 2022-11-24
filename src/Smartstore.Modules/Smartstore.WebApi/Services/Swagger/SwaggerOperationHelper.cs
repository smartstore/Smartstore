#nullable enable

using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Smartstore.ComponentModel;
using Smartstore.Web.Api.Security;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Smartstore.Web.Api.Swagger
{
    internal class SwaggerOperationHelper
    {
        private static readonly string[] _descriptionsToReplace = new[]
        {
            "Success", "No Content", "Bad Request", "Not Found", "Client Error"
        };

        private static readonly Dictionary<int, string> _statusDescriptions = new()
        {
            { Status200OK, "The request has succeeded." },
            { Status204NoContent, "The request has succeeded. There is no content provided." },
            { Status400BadRequest, "Bad request. The reason is assumed to be a client error, like incorrect data, data formatting or request syntax." },
            { Status401Unauthorized, $"Unauthorized API request. The exact reason is provided by the **{BasicAuthenticationHandler.ResultDescriptionHeader}** response header." },
            { Status404NotFound, "The requested resource was not found." },
            { Status409Conflict, "The request failed due to a conflict. The most common cause of this failure is a concurrency violation at the related entity." },
            { Status415UnsupportedMediaType, "The request failed due to an unsupported content type." },
            { Status422UnprocessableEntity, "The processing of the associated entity failed. Details about the reason can be found in the response message." },
            { Status500InternalServerError, "Internal server error. Indicates that the server has encountered an unexpected error." }
        };

        public SwaggerOperationHelper(OpenApiOperation operation, OperationFilterContext context)
        {
            Op = operation;
            Context = context;
        }

        public OpenApiOperation Op { get; }
        public OperationFilterContext Context { get; }

        public string ActionName
            => Context.MethodInfo.Name.EmptyNull();

        public string? HttpMethod
            => Context.ApiDescription.HttpMethod;

        /// <summary>
        /// Gets the generic entity type of the <see cref="WebApiController{TEntity}"/>.
        /// </summary>
        public Type EntityType
            => Context.MethodInfo.DeclaringType!.BaseType!.GenericTypeArguments[0];

        /// <summary>
        /// Gets the response type specified by <see cref="ProducesResponseTypeAttribute"/> (if any).
        /// </summary>
        public Type? ResponseType
        {
            get
            {
                var okType = Context?.ApiDescription?.SupportedResponseTypes
                    ?.Where(x => x.StatusCode == Status200OK && x.Type != null)
                    ?.Select(x => x.Type)
                    ?.FirstOrDefault();

                if (okType != null)
                {
                    return okType.IsGenericType
                        ? okType.GenericTypeArguments[0]
                        : okType;
                }

                return null;
            }
        }

        public bool HasKeyParameter
            => Context.ApiDescription.ActionDescriptor.Parameters.Any(x => x.Name.EqualsNoCase("key"));

        public ControllerActionDescriptor ActionDescriptor
            => (ControllerActionDescriptor)Context.ApiDescription.ActionDescriptor;

        public OpenApiSchema GenerateSchema(Type modelType)
            => Context.SchemaGenerator.GenerateSchema(modelType, Context.SchemaRepository);

        public IEnumerable<FastProperty> GetEntityProperties(Func<FastProperty, bool>? predicate = null)
        {
            var properties = FastProperty.GetProperties(EntityType).Values
                .Where(x => !x.Property.HasAttribute<NotMappedAttribute>(false) && !x.Property.HasAttribute<IgnoreDataMemberAttribute>(false));

            return predicate != null ? properties.Where(predicate) : properties;
        }

        public OpenApiRequestBody CreateRequestBody()
        {
            var required = true;
            string? description = null;

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
                    [Json] = new OpenApiMediaType
                    {
                        Schema = GenerateSchema(EntityType)
                    }
                }
            };
        }

        public OpenApiResponse CreateSucccessResponse(bool isSingleResult, Type? entityType = null)
        {
            entityType ??= ResponseType ?? EntityType;
            var modelType = isSingleResult ? entityType : typeof(IQueryable<>).MakeGenericType(entityType);

            var description = "The request has succeeded.";

            description += isSingleResult
                ? $" Returns details of {entityType.Name}."
                : $" Returns a list of {entityType.Name}.";

            if (ActionName == "Put" || ActionName == "Patch")
            {
                description += " This response is only returned if the HTTP header **Prefer** with the value **return=representation** is sent.";
            }

            return new OpenApiResponse
            {
                Description = description,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    [Json] = new OpenApiMediaType
                    {
                        Schema = GenerateSchema(modelType)
                    }
                }
            };
        }

        //public OpenApiResponse CreateSucccessResponse(Type modelType)
        //{
        //    var returnStr = modelType == null
        //        ? "Returns a value of varying type."
        //        : $"Returns a value of type {modelType.Name}";

        //    // "string" is fine because it is just an example value.
        //    modelType ??= typeof(string);

        //    return new OpenApiResponse
        //    {
        //        Description = $"The request has succeeded. {returnStr}",
        //        Content = new Dictionary<string, OpenApiMediaType>
        //        {
        //            [Json] = new OpenApiMediaType
        //            {
        //                Schema = GenerateSchema(modelType)
        //            }
        //        }
        //    };
        //}

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

        public void AddResponse(params int[] statusCodes)
        {
            foreach (var statusCode in statusCodes)
            {
                if (_statusDescriptions.TryGetValue(statusCode, out var description))
                {
                    Op.Responses[statusCode.ToString()] = new() { Description = description };
                }
            }
        }

        public void ReplaceResponseDescriptions()
        {
            var responses = Op.Responses.Where(x => _descriptionsToReplace.Contains(x.Value.Description, StringComparer.OrdinalIgnoreCase));

            foreach (var response in responses)
            {
                if (int.TryParse(response.Key, out int statusCode) && _statusDescriptions.TryGetValue(statusCode, out var description))
                {
                    response.Value.Description = description;
                }
            }
        }

        public string? BuildQueryExample(AllowedQueryOptions option)
        {
            string? example = null;
            FastProperty? prop;

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

                    example ??= "Name eq 'iPhone Plus'";
                    break;
                case AllowedQueryOptions.Expand:
                    prop = GetEntityProperties(x => x.Property.PropertyType.IsClosedGenericTypeOf(typeof(ICollection<>))).FirstOrDefault();
                    example = prop?.Property?.Name ?? "TierPrices";
                    break;
                case AllowedQueryOptions.Select:
                    prop = GetEntityProperties(x => x.Property.PropertyType.IsBasicType()).FirstOrDefault();
                    example = prop?.Property?.Name ?? "Name";
                    break;
                case AllowedQueryOptions.OrderBy:
                    prop = GetEntityProperties(x => x.Property.PropertyType.IsBasicType()).FirstOrDefault();
                    example = prop?.Property?.Name ?? "Name" + " desc";
                    break;
                case AllowedQueryOptions.Top:
                    example = "50";
                    break;
                case AllowedQueryOptions.Skip:
                    example = "200";
                    break;
                case AllowedQueryOptions.Count:
                    example = "true";
                    break;
                case AllowedQueryOptions.Compute:
                    example = "Price mul OrderMinimumQuantity as MinSpentPrice&$select=MinSpentPrice";
                    break;
                case AllowedQueryOptions.Search:
                    example = "blue OR green";
                    break;
                default:
                    return null;
            }

            return $"Example: **{example}**.";
        }
    }
}
