using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Smartstore.Web.Api.Security;
using Swashbuckle.AspNetCore.SwaggerGen;
using static Microsoft.ClearScript.V8.V8CpuProfile;

namespace Smartstore.Web.Api.Swagger
{
    /// <summary>
    /// Adds information to <see cref="OpenApiOperation"/> like describing <see cref="OpenApiResponse"/> objects
    /// for repeating methods in OData controllers (like Get, Post, Put, Patch, Delete etc.).
    /// Fixes lots of Swashbuckle bugs.
    /// Only takes into account OData controllers that inherit from SmartODataController.
    /// </summary>
    internal class SwaggerOperationFilter : IOperationFilter
    {
        private static readonly string[] _knownMethodNames = new[] { "Get", "Post", "Put", "Patch", "Delete" };
        private static readonly Type[] _parametersToRemove = new[] { typeof(ODataQueryOptions) };

        private static readonly AllowedQueryOptions[] _supportedQueryOptions = new[]
        {
            AllowedQueryOptions.Top,
            AllowedQueryOptions.Skip,
            AllowedQueryOptions.OrderBy,
            AllowedQueryOptions.Count,
            AllowedQueryOptions.Select,
            AllowedQueryOptions.Expand,
            AllowedQueryOptions.Filter,
            AllowedQueryOptions.Search,
            AllowedQueryOptions.Compute
        };

        // Perf: avoids multiple creation of the same query parameters.
        private readonly Dictionary<string, OpenApiParameter> _queryParameters = new();

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            try
            {
                // Skip what is not inherited from our SmartODataController.
                if (context.MethodInfo.DeclaringType.BaseType.IsClosedGenericTypeOf(typeof(SmartODataController<>)))
                {
                    var helper = new SwaggerOperationHelper(operation, context);

                    AddOperationInfo(helper);
                    AddParameterInfo(helper);
                    AddQueryParameters(helper);
                    FixOdataActions(helper);
                    FixOdataFunctions(helper);
                    RemoveParameters(helper);
                    // Probably getting obsolete:
                    ApplyConsumesExample(helper);
                }
            }
            catch (Exception ex)
            {
                ex.Dump();
            }
        }

        /// <summary>
        /// Adds info to known OData methods.
        /// </summary>
        protected virtual void AddOperationInfo(SwaggerOperationHelper helper)
        {
            var mi = helper.Context.MethodInfo;
            var isSingleResult = mi.ReturnType.IsClosedGenericTypeOf(typeof(SingleResult<>));
            var isQueryResult = mi.ReturnType.IsClosedGenericTypeOf(typeof(IQueryable<>));
            var isNavigationProperty = false;
            var canProcess = _knownMethodNames.Contains(mi.Name);

            if (!canProcess && helper.ActionName.StartsWith("Get") && helper.HttpMethod.EqualsNoCase("Get"))
            {
                isNavigationProperty = isSingleResult || isQueryResult;
                canProcess = isNavigationProperty;
            }

            if (canProcess)
            {
                helper.Op.Responses.Clear();
            }

            if (mi.DeclaringType.HasAttribute<AuthorizeAttribute>(true) || mi.HasAttribute<AuthorizeAttribute>(true))
            {
                helper.Op.Responses[Status401Unauthorized.ToString()] = CreateUnauthorizedResponse();
            }

            if (!canProcess)
            {
                return;
            }

            helper.Op.Responses[Status400BadRequest.ToString()] = CreateBadRequestResponse();

            var entityName = PrefixArticle(helper.EntityAliasName);

            switch (helper.ActionName)
            {
                case "Get":
                    if (helper.HasKeyParameter)
                    {
                        helper.Op.Summary ??= $"Gets {entityName} by identifier.";
                        helper.Op.Responses[Status200OK.ToString()] = helper.CreateSucccessResponse(true);
                        helper.Op.Responses[Status404NotFound.ToString()] = CreateNotFoundResponse();
                        helper.AddKeyParameter();
                    }
                    else
                    {
                        helper.Op.Summary ??= $"Gets {entityName} list.";
                        helper.Op.Responses[Status200OK.ToString()] = helper.CreateSucccessResponse(false);
                    }
                    break;

                //case "GetProperty":
                //    helper.Op.Summary ??= $"Gets a property value of a {helper.EntityType.Name}.";
                //    helper.Op.Description ??= "A property value can alternatively be obtained using the **$select** query string parameter.";
                //    helper.Op.Responses[Status200OK.ToString()] = helper.CreateSucccessResponse(null);
                //    helper.Op.Responses[Status404NotFound.ToString()] = CreateNotFoundResponse();
                //    helper.AddKeyParameter();
                //    helper.AddPropertyParameter();
                //    break;

                case "Post":
                    helper.Op.Summary ??= $"Creates {entityName}.";
                    helper.Op.RequestBody = helper.CreateRequestBody();
                    helper.Op.Responses[Status201Created.ToString()] = helper.CreateSucccessResponse(true);
                    break;

                case "Put":
                case "Patch":
                    helper.Op.Summary ??= (helper.ActionName == "Patch"
                        ? $"Partially updates {entityName}."
                        : $"Updates {entityName}.");

                    helper.Op.RequestBody = helper.CreateRequestBody();
                    helper.Op.Responses[Status200OK.ToString()] = helper.CreateSucccessResponse(true);
                    helper.Op.Responses[Status204NoContent.ToString()] = CreateNoContentResponse();
                    helper.Op.Responses[Status404NotFound.ToString()] = CreateNotFoundResponse();
                    helper.Op.Responses[Status409Conflict.ToString()] = CreateConflictResponse();
                    helper.Op.Responses[Status422UnprocessableEntity.ToString()] = CreateUnprocessableEntityResponse();
                    helper.AddKeyParameter();
                    break;

                case "Delete":
                    helper.Op.Summary ??= $"Deletes {entityName}.";
                    helper.Op.Responses[Status204NoContent.ToString()] = CreateNoContentResponse();
                    helper.Op.Responses[Status404NotFound.ToString()] = CreateNotFoundResponse();
                    helper.AddKeyParameter();
                    break;

                default:
                    if (isNavigationProperty)
                    {
                        var navPropType = mi.ReturnType.GenericTypeArguments[0];                        

                        helper.Op.Summary ??= isQueryResult
                            ? $"Gets {navPropType.Name.NaIfEmpty()} entities assigned to {entityName}."
                            : $"Gets the {navPropType.Name.NaIfEmpty()} assigned to {entityName}.";

                        helper.Op.Responses[Status200OK.ToString()] = helper.CreateSucccessResponse(isSingleResult, navPropType);

                        if (isSingleResult)
                        {
                            helper.Op.Responses[Status404NotFound.ToString()] = CreateNotFoundResponse();
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Adds extra info to known parameters.
        /// </summary>
        protected virtual void AddParameterInfo(SwaggerOperationHelper helper)
        {
            foreach (var parameter in helper.Op.Parameters)
            {
                switch (parameter.Name?.ToLower())
                {
                    case "key":
                    case "id":
                        parameter.Description ??= $"The {helper.EntityAliasName} identifier.";
                        parameter.Example ??= new OpenApiInteger(1234);
                        break;
                }
            }
        }

        /// <summary>
        /// Adds OData query parameters.
        /// </summary>
        protected virtual void AddQueryParameters(SwaggerOperationHelper helper)
        {
            if (!helper.HttpMethod.EqualsNoCase("Get"))
            {
                return;
            }

            var attribute = helper.ActionDescriptor?.FilterDescriptors
                .Where(x => x.Filter is EnableQueryAttribute)
                .Select(x => x.Filter as EnableQueryAttribute)
                .FirstOrDefault();

            if (attribute == null)
            {
                return;
            }

            var allowedOptions = _supportedQueryOptions.Where(x => attribute.AllowedQueryOptions.HasFlag(x));
            if (helper.HasKeyParameter)
            {
                allowedOptions = allowedOptions.Where(x => x == AllowedQueryOptions.Select || x == AllowedQueryOptions.Compute);
            }

            foreach (var option in allowedOptions)
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

        /// <summary>
        /// Adds missing request body. Removes parameters for FromODataBody that are falsely offered as query parameters.
        /// </summary>
        protected virtual void FixOdataActions(SwaggerOperationHelper helper)
        {
            if (!helper.HttpMethod.EqualsNoCase("Post") || helper.Op.RequestBody != null)
            {
                return;
            }

            var consumesJson = helper.ActionDescriptor.FilterDescriptors
                .Where(x => x.Filter is ConsumesAttribute)
                .Select(x => x.Filter as ConsumesAttribute)
                .FirstOrDefault(x => x.ContentTypes.Any(type => type.EqualsNoCase(Json)));

            if (consumesJson == null)
            {
                return;
            }

            var odataParams = helper.ActionDescriptor.Parameters
                .Select(p => p as ControllerParameterDescriptor)
                .Where(p => p?.ParameterInfo?.CustomAttributes?.Any(a => a.AttributeType == typeof(FromODataBodyAttribute)) ?? false)
                .ToList();

            if (odataParams.Count == 0)
            {
                return;
            }

            var swaggerParams = helper.Op.Parameters
                .Where(p => p.In == ParameterLocation.Query && odataParams.Any(d => d.Name == p.Name))
                .ToDictionarySafe(p => p.Name, p => p);

            if (swaggerParams.Count == 0)
            {
                return;
            }

            // Add request body.
            var bodyType = new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Type = "object",
                    Required = new HashSet<string>(),
                    Properties = new Dictionary<string, OpenApiSchema>()
                }
            };

            foreach (var p in odataParams)
            {
                if (swaggerParams.TryGetValue(p.Name, out var swaggerParam))
                {
                    bodyType.Schema.Properties[p.Name] = new OpenApiSchema
                    {
                        Type = swaggerParam.Schema?.Type ?? "string",
                        Description = swaggerParam.Description,
                        Example = swaggerParam.Example
                    };

                    if (swaggerParam.Required)
                    {
                        bodyType.Schema.Required.Add(p.Name);
                    }
                }
            }

            helper.Op.RequestBody = new OpenApiRequestBody
            {
                Required = bodyType.Schema.Required.Count > 0,
                Description = string.Join(" ", bodyType.Schema.Properties.Select(p => $"**{p.Key}**: {FirstCharToLower(p.Value.Description)}")),
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    { Json, bodyType }
                }
            };

            // Remove falsely query parameters.
            swaggerParams.Each(p => helper.Op.Parameters.Remove(p.Value));
        }

        protected virtual void FixOdataFunctions(SwaggerOperationHelper helper)
        {
            if (!helper.HttpMethod.EqualsNoCase("Get"))
            {
                return;
            }

            var odataParams = helper.ActionDescriptor.Parameters
                .Select(p => p as ControllerParameterDescriptor)
                .Where(p => p?.ParameterInfo?.CustomAttributes?.Any(a => a.AttributeType == typeof(FromODataUriAttribute)) ?? false)
                .ToList();

            if (odataParams.Count == 0)
            {
                return;
            }

            var swaggerParams = helper.Op.Parameters
                .Where(p => p.In == ParameterLocation.Query && p.Schema?.Type == "array")
                .ToDictionarySafe(p => p.Name, p => p);

            if (swaggerParams.Count == 0)
            {
                return;
            }

            foreach (var p in odataParams)
            {
                if (swaggerParams.TryGetValue(p.Name, out var swaggerParam))
                {
                    swaggerParam.Explode = false;
                    swaggerParam.In = ParameterLocation.Path;
                    swaggerParam.Schema = new OpenApiSchema { Type = "string" };

                    if (swaggerParam.Example is OpenApiArray arr && arr.Count > 0)
                    {
                        var fixedExample = string.Join(",", arr.Select(x =>
                        {
                            // TODO: (mg) (core) use\check IOpenApiPrimitive and cast to OpenApiPrimitive<T>?
                            if (x is OpenApiInteger intVal)
                                return intVal.Value.ToString();
                            else if (x is OpenApiString strVal)
                                return strVal.Value;
                            else if (x is OpenApiDouble doubleVal)
                                return doubleVal.Value.ToString();
                            else if (x is OpenApiFloat floatVal)
                                return floatVal.Value.ToString();
                            else if (x is OpenApiBoolean boolVal)
                                return boolVal.Value.ToString().ToLower();
                            else if (x is OpenApiByte byteVal)
                                return byteVal.Value.ToString();
                            else
                                return string.Empty;
                        }));

                        swaggerParam.Example = new OpenApiString('[' + fixedExample + ']');
                    }
                }
            }
        }

        /// <summary>
        /// Removes unwanted parameters.
        /// </summary>
        protected virtual void RemoveParameters(SwaggerOperationHelper helper)
        {
            helper.Op.Parameters.Remove(p =>
            {
                var refId = p?.Schema?.Reference?.Id;
                return refId != null && _parametersToRemove.Any(type => refId.StartsWithNoCase(type.FullName));
            });
        }

        /// <summary>
        /// Applies properties of <see cref="ApiConsumesAttribute"/> to <see cref="OpenApiOperation.RequestBody"/>.
        /// Could become obsolete once Swashbuckle can do it.
        /// </summary>
        protected virtual void ApplyConsumesExample(SwaggerOperationHelper helper)
        {
            //if (helper.HttpMethod.EqualsNoCase("POST")
            //    && helper.ActionDescriptor.Parameters.Any(x => x.ParameterType == typeof(ODataActionParameters)))

            if (helper.ActionName.EqualsNoCase("SaveFile"))
            {
                //var mediaType = new OpenApiMediaType
                //{
                //    Schema = new OpenApiSchema
                //    {
                //        Type = "object",
                //        Required = new HashSet<string> { "File" },
                //        Properties = new Dictionary<string, OpenApiSchema>
                //        {
                //            {
                //                "File", new OpenApiSchema
                //                {
                //                    Type = "string",
                //                    Format = "binary"
                //                }
                //            }
                //        }
                //    }
                //};

                //var content = new Dictionary<string, OpenApiMediaType>
                //{
                //    { "multipart/form-data", mediaType }
                //};

                //helper.Op.RequestBody = new OpenApiRequestBody
                //{
                //    Content = content
                //};
            }

            var body = helper.Op.RequestBody;
            if (body == null)
            {
                return;
            }

            var attribute = (ApiConsumesAttribute)helper.ActionDescriptor.MethodInfo.GetCustomAttributes(typeof(ApiConsumesAttribute), false).FirstOrDefault();
            if (attribute == null)
            {
                return;
            }

            body.Required = attribute.Required;

            foreach (var contentType in attribute.ContentTypes)
            {
                if (body.Content.TryGetValue(contentType, out var mediaType))
                {
                    if (attribute.SchemaType != null)
                    {
                        mediaType.Schema = helper.GenerateSchema(attribute.SchemaType);
                    }
                    
                    if (mediaType.Example == null && attribute.Example.HasValue())
                    {
                        mediaType.Example = new OpenApiString(attribute.Example);
                    }
                }
            }
        }

        #region Utilities

        private static OpenApiResponse CreateUnauthorizedResponse()
            => new() { Description = $"Unauthorized API request. The exact reason is provided by the **{BasicAuthenticationHandler.ResultDescriptionHeader}** response header." };

        private static OpenApiResponse CreateNotFoundResponse()
            => new() { Description = "The requested resource was not found." };

        private static OpenApiResponse CreateBadRequestResponse()
            => new() { Description = "Bad request. Occurs, for example, when the data sent is incorrect." };

        private static OpenApiResponse CreateNoContentResponse()
            => new() { Description = "The request has succeeded. There is no content provided." };

        private static OpenApiResponse CreateConflictResponse()
            => new() { Description = "The request failed due to a conflict. The most common cause of this failure is a concurrency violation at the related entity." };

        private static OpenApiResponse CreateUnprocessableEntityResponse()
            => new() { Description = "The processing of the associated entity failed. Details about the reason can be found in the response message." };

        private static string PrefixArticle(string str)
        {
            return str.HasValue()
                ? ("aeiouAEIOU".Contains(str[0]) ? "an " : "a ") + str
                : str;
        }

        private static string FirstCharToLower(string str)
        {
            return str.HasValue() && char.IsUpper(str[0])
                ? str.Length == 1 ? char.ToLower(str[0]).ToString() : char.ToLower(str[0]) + str[1..]
                : str;
        }

        #endregion
    }
}
