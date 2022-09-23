using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Smartstore.ComponentModel;
using Smartstore.Web.Api.Security;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Smartstore.Web.Api.Swagger
{
    /// <summary>
    /// Adds information to <see cref="OpenApiOperation"/> like describing <see cref="OpenApiResponse"/> objects
    /// for repeating methods in OData controllers (like GetProperty, Get, Post etc.).
    /// Only takes into account OData controllers that inherit from <see cref="SmartODataController<>"/>.
    /// </summary>
    public class SwaggerOperationFilter : IOperationFilter
    {
        private static readonly string[] _candidateMethodNames = new[] { "Get", "GetProperty", "Post", "Put", "Patch", "Delete" };

        private static readonly AllowedQueryOptions[] _supportedQueryOptions = new[]
        {
            AllowedQueryOptions.Top,
            AllowedQueryOptions.Skip,
            AllowedQueryOptions.Count,
            AllowedQueryOptions.Compute,
            AllowedQueryOptions.OrderBy,
            AllowedQueryOptions.Select,
            AllowedQueryOptions.Expand,
            AllowedQueryOptions.Filter,
            AllowedQueryOptions.Search
        };

        private readonly Dictionary<string, OpenApiParameter> _queryParameters = new();

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            try
            {
                // Skip what is not inherited from our SmartODataController.
                if (context.MethodInfo.DeclaringType.BaseType.IsClosedGenericTypeOf(typeof(SmartODataController<>)))
                {
                    var helper = new SwaggerOperationHelper(operation, context);
                    
                    ProcessMethods(helper);
                    AddQueryParameters(helper);
                }
            }
            catch
            {
            }
        }

        protected virtual void ProcessMethods(SwaggerOperationHelper helper)
        {
            var mi = helper.Context.MethodInfo;
            var isCandidate = _candidateMethodNames.Contains(mi.Name);

            if (isCandidate)
            {
                helper.Op.Responses.Clear();
            }

            if (mi.DeclaringType.HasAttribute<AuthorizeAttribute>(true) || mi.HasAttribute<AuthorizeAttribute>(true))
            {
                helper.Op.Responses[StatusCodes.Status401Unauthorized.ToString()] = CreateUnauthorizedResponse();
            }

            if (!isCandidate)
            {
                return;
            }

            switch (helper.ActionName)
            {
                case "Get":
                    if (mi.ReturnType.IsClosedGenericTypeOf(typeof(IQueryable<>)))
                    {
                        helper.Op.Summary ??= $"Gets a {helper.EntityType.Name} list.";
                        helper.Op.Responses[StatusCodes.Status200OK.ToString()] = helper.CreateSucccessResponse(false);
                    }
                    else
                    {
                        helper.Op.Summary ??= $"Gets a {helper.EntityType.Name} by identifier.";
                        helper.Op.Responses[StatusCodes.Status200OK.ToString()] = helper.CreateSucccessResponse(true);
                        helper.Op.Responses[StatusCodes.Status404NotFound.ToString()] = CreateNotFoundResponse();

                        helper.AddKeyParameter();
                    }
                    break;

                case "GetProperty":
                    helper.Op.Summary ??= $"Gets a property value of a {helper.EntityType.Name}.";
                    helper.Op.Description ??= "A property value can alternatively be obtained using the **$select** query string parameter.";
                    helper.Op.Responses[StatusCodes.Status200OK.ToString()] = helper.CreateSucccessResponse(null);
                    helper.Op.Responses[StatusCodes.Status404NotFound.ToString()] = CreateNotFoundResponse();

                    helper.AddKeyParameter();
                    helper.AddPropertyParameter();
                    break;

                case "Post":
                    helper.Op.Summary ??= $"Creates a new {helper.EntityType.Name}.";
                    helper.Op.RequestBody = helper.CreateRequestBody();
                    helper.Op.Responses[StatusCodes.Status201Created.ToString()] = helper.CreateSucccessResponse(true);
                    helper.Op.Responses[StatusCodes.Status400BadRequest.ToString()] = CreateBadRequestResponse();
                    break;

                case "Put":
                case "Patch":
                    helper.Op.Summary ??= (helper.ActionName == "Patch"
                        ? $"Partially updates a {helper.EntityType.Name}."
                        : $"Updates a {helper.EntityType.Name}.");

                    helper.Op.RequestBody = helper.CreateRequestBody();
                    helper.Op.Responses[StatusCodes.Status200OK.ToString()] = helper.CreateSucccessResponse(true);
                    helper.Op.Responses[StatusCodes.Status204NoContent.ToString()] = CreateNoContentResponse();
                    helper.Op.Responses[StatusCodes.Status400BadRequest.ToString()] = CreateBadRequestResponse();
                    helper.Op.Responses[StatusCodes.Status404NotFound.ToString()] = CreateNotFoundResponse();
                    helper.Op.Responses[StatusCodes.Status409Conflict.ToString()] = CreateConflictResponse();
                    helper.Op.Responses[StatusCodes.Status422UnprocessableEntity.ToString()] = CreateUnprocessableEntityResponse();

                    helper.AddKeyParameter();
                    break;

                case "Delete":
                    helper.Op.Summary ??= $"Deletes a {helper.EntityType.Name}.";
                    helper.Op.Responses[StatusCodes.Status204NoContent.ToString()] = CreateNoContentResponse();
                    helper.Op.Responses[StatusCodes.Status404NotFound.ToString()] = CreateNotFoundResponse();

                    helper.AddKeyParameter();
                    break;
            }
        }

        protected virtual void AddQueryParameters(SwaggerOperationHelper helper)
        {
            var descriptor = helper.Context.ApiDescription.ActionDescriptor as ControllerActionDescriptor;
            var attribute = descriptor?.FilterDescriptors
                .Where(x => x.Filter is EnableQueryAttribute)
                .Select(x => x.Filter as EnableQueryAttribute)
                .FirstOrDefault();

            if (attribute == null)
            {
                return;
            }

            foreach (var option in _supportedQueryOptions.Where(x => attribute.AllowedQueryOptions.HasFlag(x)))
            {
                var key = helper.EntityType.FullName + option.ToString();

                if (!_queryParameters.TryGetValue(key, out var p))
                {
                    p = new OpenApiParameter
                    {
                        Required = false,
                        In = ParameterLocation.Query
                    };

                    // INFO: OpenApiParameter.Example is not suitable for optional inputs. It remains in the input field when Try-It-Out is clicked.
                    // The user would have to delete or update it for all filters before execution.
                    var example = helper.BuildQueryExample(option).EmptyNull();

                    switch (option)
                    {
                        case AllowedQueryOptions.Filter:
                            p.Name = "$filter";
                            p.Description = "Expression(s) to limit the results to be returned. " + example;
                            break;
                        case AllowedQueryOptions.Expand:
                            p.Name = "$expand";
                            p.Description = "A comma-separated list of related entities to be included inline in the response. " + example;
                            break;
                        case AllowedQueryOptions.Select:
                            p.Name = "$select";
                            p.Description = "A comma-separated list of properties to be included in the response. " + example;
                            break;
                        case AllowedQueryOptions.OrderBy:
                            p.Name = "$orderby";
                            p.Description = "A comma-separated list of expression(s) to order the returned entities. " + example;
                            break;
                        case AllowedQueryOptions.Top:
                            p.Name = "$top";
                            p.Description = "The number of returned entities. " + example;
                            p.Schema ??= helper.GenerateSchema(typeof(int));
                            break;
                        case AllowedQueryOptions.Skip:
                            p.Name = "$skip";
                            p.Description = "The number of skipped and not included entities. " + example;
                            p.Schema ??= helper.GenerateSchema(typeof(int));
                            break;
                        case AllowedQueryOptions.Count:
                            p.Name = "$count";
                            p.Description = "Requests the total number of matching entities included in the response. " + example;
                            p.Schema ??= helper.GenerateSchema(typeof(bool));
                            break;
                        case AllowedQueryOptions.Compute:
                            p.Name = "$compute";
                            p.Description = "Defines a computed property that can be used in a $select, $filter or $orderby expression.<br>" + example;
                            break;
                        case AllowedQueryOptions.Search:
                            // TODO (?): $search can be empowered by own matching logic by implementing ISearchBinder.
                            // https://devblogs.microsoft.com/odata/compute-and-search-in-asp-net-core-odata-8/
                            p.Name = "$search";
                            p.Description = "Expression to limit the results matching a free-text search. The definition of what *matching* means is up to the service. " + example;
                            break;
                    }

                    p.Schema ??= helper.GenerateSchema(typeof(string));

                    _queryParameters[key] = p;
                }

                helper.Op.Parameters.Add(p);
            }            
        }

        #region Utilities

        private static OpenApiResponse CreateUnauthorizedResponse()
            => new() { Description = $"Unauthorized API request. The exact reason is provided by the **{BasicAuthenticationHandler.ResultDescriptionHeader}** response header." };

        private static OpenApiResponse CreateNotFoundResponse()
            => new() { Description = "The requested resource was not found." };

        private static OpenApiResponse CreateBadRequestResponse()
            => new() { Description = "Bad request, e.g. because something is wrong with the sent data." };

        private static OpenApiResponse CreateNoContentResponse()
            => new() { Description = "The request has succeeded. There is no content provided." };

        private static OpenApiResponse CreateConflictResponse()
            => new() { Description = "The request failed due to a conflict. The most common cause of this failure is a concurrency violation at the related entity." };

        private static OpenApiResponse CreateUnprocessableEntityResponse()
            => new() { Description = "The processing of the associated entity failed. Details about the reason can be found in the response message." };

        #endregion
    }

    public class SwaggerOperationHelper
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
                .Where(x => !x.Property.HasAttribute<NotMappedAttribute>(false) && !x.Property.HasAttribute<JsonIgnoreAttribute>(false));

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
