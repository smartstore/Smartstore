#nullable enable

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

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
        private static readonly Regex _pathsToIgnore = new(@"\/default\.",
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
                if (context.MethodInfo?.DeclaringType?.BaseType?.IsClosedGenericTypeOf(typeof(SmartODataController<>)) ?? false)
                {
                    if (!_pathsToIgnore.IsMatch(context.ApiDescription.RelativePath!))
                    {
                        var helper = new SwaggerOperationHelper(operation, context);

                        AddOperationInfo(helper);
                        AddParameterInfo(helper);
                        AddQueryParameters(helper);
                        FixOdataActions(helper);
                        FixOdataFunctions(helper);
                        RemoveParameters(helper);
                    }
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

            helper.ReplaceResponseDescriptions();
            // Add responses that can theoretically occur on any endpoint.
            helper.AddResponse(Status400BadRequest, Status500InternalServerError);

            if (mi.DeclaringType.HasAttribute<AuthorizeAttribute>(true) || mi.HasAttribute<AuthorizeAttribute>(true))
            {
                helper.AddResponse(Status401Unauthorized);
            }

            if (!canProcess)
            {
                return;
            }

            var entityName = PrefixArticle(helper.EntityType.Name);

            switch (helper.ActionName)
            {
                case "Get":
                    var responseTypeName = PrefixArticle(helper.ResponseType?.Name) ?? entityName;
                    if (helper.HasKeyParameter)
                    {
                        helper.Op.Summary ??= $"Gets {responseTypeName} by identifier.";
                        helper.AddResponse(Status404NotFound);
                        helper.AddKeyParameter();
                    }
                    else
                    {
                        helper.Op.Summary ??= $"Gets {responseTypeName} list.";
                    }
                    helper.Op.Responses[Status200OK.ToString()] = helper.CreateSucccessResponse(helper.HasKeyParameter);
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
                    helper.AddResponse(Status204NoContent, Status404NotFound, Status409Conflict, Status422UnprocessableEntity);
                    helper.AddKeyParameter();
                    break;

                case "Delete":
                    helper.Op.Summary ??= $"Deletes {entityName}.";
                    helper.AddResponse(Status204NoContent, Status404NotFound);
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
                            helper.AddResponse(Status404NotFound);
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
                        parameter.Description ??= $"The {helper.EntityType.Name} identifier.";
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
                allowedOptions = allowedOptions.Where(x => 
                    x == AllowedQueryOptions.Select || 
                    x == AllowedQueryOptions.Expand ||
                    x == AllowedQueryOptions.Compute);
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

        ///// <summary>
        ///// Adds a file uploader for methods decorated with <see cref="ApiUploadAttribute"/>.
        ///// </summary>
        //protected virtual void AddUploader(SwaggerOperationHelper helper)
        //{
        //    var attribute = (ApiUploadAttribute)helper.Context.MethodInfo.GetCustomAttributes(typeof(ApiUploadAttribute), false).FirstOrDefault();
        //    if (attribute == null)
        //    {
        //        return;
        //    }

        //    attribute.PropertyName ??= "file";

        //    var mediaType = new OpenApiMediaType
        //    {
        //        Schema = new OpenApiSchema
        //        {
        //            Type = "object",
        //            Required = new HashSet<string> { attribute.PropertyName },
        //            Properties = new Dictionary<string, OpenApiSchema>
        //            {
        //                {
        //                    attribute.PropertyName, new OpenApiSchema
        //                    {
        //                        Type = "string",
        //                        Format = "binary"
        //                    }
        //                }
        //            }
        //        }
        //    };

        //    var content = new Dictionary<string, OpenApiMediaType>
        //    {
        //        { attribute.ContentTypes.FirstOrDefault() ?? "multipart/form-data", mediaType }
        //    };

        //    helper.Op.RequestBody = new OpenApiRequestBody
        //    {
        //        Content = content,
        //        Required = !attribute.IsOptional
        //    };

        //    //foreach (var contentType in attribute.ContentTypes)
        //    //{
        //    //    if (body.Content.TryGetValue(contentType, out var mediaType))
        //    //    {
        //    //        if (attribute.SchemaType != null)
        //    //        {
        //    //            mediaType.Schema = helper.GenerateSchema(attribute.SchemaType);
        //    //        }

        //    //        if (mediaType.Example == null && attribute.Example.HasValue())
        //    //        {
        //    //            mediaType.Example = new OpenApiString(attribute.Example);
        //    //        }
        //    //    }
        //    //}
        //}

        //protected virtual void FixUploads(SwaggerOperationHelper helper)
        //{
        //    if (!helper.HttpMethod.EqualsNoCase("Post") || helper.Op.RequestBody == null)
        //    {
        //        return;
        //    }

        //    var consumesForm = helper.ActionDescriptor.FilterDescriptors
        //        .Where(x => x.Filter is ConsumesAttribute)
        //        .Select(x => x.Filter as ConsumesAttribute)
        //        .FirstOrDefault(x => x.ContentTypes.Any(type => type.EqualsNoCase("multipart/form-data")));

        //    if (consumesForm == null)
        //    {
        //        return;
        //    }

        //    var actionParams = helper.ActionDescriptor.Parameters
        //        .Select(p => p as ControllerParameterDescriptor)
        //        .ToList();

        //    if (actionParams.Count == 0)
        //    {
        //        return;
        //    }

        //    var swaggerParams = helper.Op.Parameters
        //        .Where(p => actionParams.Any(d => d.Name == p.Name))
        //        .ToDictionarySafe(p => p.Name, p => p);

        //    if (swaggerParams.Count == 0)
        //    {
        //        return;
        //    }

        //    var body = helper.Op.RequestBody.Content["multipart/form-data"];

        //    foreach (var p in actionParams)
        //    {
        //        if (swaggerParams.TryGetValue(p.Name, out var swaggerParam))
        //        {
        //            var type = swaggerParam.Schema?.Type ?? MapType(p.ParameterType);

        //            var schema = type.EqualsNoCase("object") && swaggerParam.Example == null
        //                ? helper.GenerateSchema(p.ParameterType)
        //                : new OpenApiSchema { Type = type };

        //            schema.Example = swaggerParam.Example;
        //            schema.Description = swaggerParam.Description;

        //            body.Schema.Properties[p.Name] = schema;

        //            if (swaggerParam.Required && !body.Schema.Required.Contains(p.Name))
        //            {
        //                body.Schema.Required.Add(p.Name);
        //            }
        //        }
        //    }

        //    //var description = string.Join("<br><br>", body.Schema.Properties.Select(p => $"**{p.Key}**: {FirstCharToLower(p.Value.Description)}"));
        //    //helper.Op.RequestBody.Description = helper.Op.RequestBody.Description.Grow(description, "<br><br>");

        //    // Remove falsely query parameters.
        //    swaggerParams.Each(p => helper.Op.Parameters.Remove(p.Value));
        //}

        /// <summary>
        /// Adds missing request body. Removes parameters for FromODataBody that are falsely offered as query parameters.
        /// </summary>
        /// <remarks>This is a workaround that will probably become obsolete in future Swashbuckle versions.</remarks>
        protected virtual void FixOdataActions(SwaggerOperationHelper helper)
        {
            if (!helper.HttpMethod.EqualsNoCase("Post") || helper.Op.RequestBody != null)
            {
                return;
            }

            var consumesJson = helper.ActionDescriptor.FilterDescriptors
                .Where(x => x.Filter is ConsumesAttribute)
                .Select(x => (ConsumesAttribute)x.Filter)
                .FirstOrDefault(x => x.ContentTypes.Any(type => type.EqualsNoCase(Json)));

            if (consumesJson == null)
            {
                return;
            }

            var actionParams = helper.ActionDescriptor.Parameters
                .Where(p => p is ControllerParameterDescriptor)
                .Select(p => (ControllerParameterDescriptor)p)
                .Where(p => p.ParameterInfo?.CustomAttributes?.Any(a => a.AttributeType == typeof(FromODataBodyAttribute)) ?? false)
                .ToList();

            if (actionParams.Count == 0)
            {
                return;
            }

            var swaggerParams = helper.Op.Parameters
                .Where(p => p.In == ParameterLocation.Query && actionParams.Any(d => d.Name == p.Name))
                .ToDictionarySafe(p => p.Name, p => p);

            if (swaggerParams.Count == 0)
            {
                return;
            }

            // Add request body.
            var body = new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Type = "object",
                    Required = new HashSet<string>(),
                    Properties = new Dictionary<string, OpenApiSchema>()
                }
            };

            foreach (var p in actionParams)
            {
                if (swaggerParams.TryGetValue(p.Name, out var swaggerParam))
                {
                    var type = swaggerParam.Schema?.Type ?? MapType(p.ParameterType);

                    var schema = type.EqualsNoCase("object") && swaggerParam.Example == null
                        ? helper.GenerateSchema(p.ParameterType)
                        : new OpenApiSchema { Type = type };

                    schema.Example = swaggerParam.Example;
                    schema.Description = swaggerParam.Description;

                    body.Schema.Properties[p.Name] = schema;

                    if (swaggerParam.Required)
                    {
                        body.Schema.Required.Add(p.Name);
                    }
                }
            }

            helper.Op.RequestBody = new OpenApiRequestBody
            {
                Required = body.Schema.Required.Count > 0,
                // Redundant. Just click "Schema" to see property descriptions:
                //Description = string.Join("<br><br>", body.Schema.Properties.Select(p => $"**{p.Key}**: {FirstCharToLower(p.Value.Description)}")),
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    { Json, body }
                }
            };

            // Remove falsely query parameters.
            swaggerParams.Each(p => helper.Op.Parameters.Remove(p.Value));
        }

        /// <summary>
        /// Fixes wrong identifier arrays in OData URL path.
        /// </summary>
        /// <remarks>This is a workaround that will probably become obsolete in future Swashbuckle versions.</remarks>
        protected virtual void FixOdataFunctions(SwaggerOperationHelper helper)
        {
            if (!helper.HttpMethod.EqualsNoCase("Get"))
            {
                return;
            }

            var odataParams = helper.ActionDescriptor.Parameters
                .Where(p => p is ControllerParameterDescriptor)
                .Select(p => (ControllerParameterDescriptor)p)
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
                        var fixedExample = string.Join(',', arr.Select(x => Convert(x)));
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
                return refId != null && _parametersToRemove.Any(type => refId.StartsWithNoCase(type.FullName!));
            });
        }

        #region Utilities

        private static string? PrefixArticle(string? str)
        {
            return str.HasValue()
                ? ("aeiouAEIOU".Contains(str![0]) ? "an " : "a ") + str
                : str;
        }

        //private static string FirstCharToLower(string str)
        //{
        //    return str.HasValue() && char.IsUpper(str[0])
        //        ? str.Length == 1 ? char.ToLower(str[0]).ToString() : char.ToLower(str[0]) + str[1..]
        //        : str;
        //}

        private static string Convert(IOpenApiAny value)
        {
            // Odd. Compare with OpenApiPrimitive.Write.
            if (value is OpenApiInteger intVal)
                return intVal.Value.ToString();
            else if (value is OpenApiString strVal)
                return strVal.Value;
            else if (value is OpenApiLong longVal)
                return longVal.Value.ToString();
            else if (value is OpenApiFloat floatVal)
                return floatVal.Value.ToString();
            else if (value is OpenApiDouble doubleVal)
                return doubleVal.Value.ToString();
            else if (value is OpenApiByte byteVal)
                return string.Join(',', byteVal.Value);
            else if (value is OpenApiBoolean boolVal)
                return boolVal.Value.ToString().ToLower();
            else if (value is OpenApiDate dateVal)
                return dateVal.Value.ToString();
            else if (value is OpenApiDateTime dtVal)
                return dtVal.Value.ToString();
            else
                return string.Empty;
        }

        private static string MapType(Type type)
        {
            // See https://swagger.io/docs/specification/data-models/data-types/
            if (type.IsEnum)
            {
                // Avoid ODataException: "Cannot read the value '0' as a quoted JSON string value".
                return "string";
            }
            else if (type.IsArray)
            {
                return "array";
            }
            else if (type == typeof(int))
            {
                return "integer";
            }
            else if (type == typeof(bool))
            {
                return "boolean";
            }
            else if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
            {
                return "number";
            }
            else if (!type.IsPrimitive && !typeof(IFormFile).IsAssignableFrom(type))
            {
                return "object";
            }

            // Fallback. Includes dates and files.
            return "string";
        }

        #endregion
    }
}
