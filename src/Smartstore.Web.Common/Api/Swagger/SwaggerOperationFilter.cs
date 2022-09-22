using System.Net.Mime;
using System.Reflection.Metadata;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Smartstore.ComponentModel;
using Smartstore.Web.Api.Security;
using Swashbuckle.AspNetCore.SwaggerGen;
using static Microsoft.ClearScript.V8.V8CpuProfile;

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

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            try
            {
                // Skip what is not inherited from our SmartODataController.
                if (context.MethodInfo.DeclaringType.BaseType.IsClosedGenericTypeOf(typeof(SmartODataController<>)))
                {
                    ProcessMethods(operation, context);
                    AddQueryParameters(operation, context);
                }
            }
            catch
            {
            }
        }

        protected virtual void ProcessMethods(OpenApiOperation operation, OperationFilterContext context)
        {
            var mi = context.MethodInfo;
            var isCandidate = _candidateMethodNames.Contains(mi.Name);

            if (isCandidate)
            {
                operation.Responses.Clear();
            }

            if (mi.DeclaringType.HasAttribute<AuthorizeAttribute>(true) || mi.HasAttribute<AuthorizeAttribute>(true))
            {
                operation.Responses[StatusCodes.Status401Unauthorized.ToString()] = CreateUnauthorizedResponse();
            }

            if (!isCandidate)
            {
                return;
            }

            var ctx = new OperationContext { Op = operation, Context = context };

            switch (ctx.ActionName)
            {
                case "Get":
                    if (mi.ReturnType.IsGenericType && mi.ReturnType.IsClosedGenericTypeOf(typeof(IQueryable<>)))
                    {
                        operation.Responses[StatusCodes.Status200OK.ToString()] = CreateSucccessResponse(ctx, false);

                        UpdateOperation(ctx, $"Gets a {ctx.EntityType.Name} list.");
                    }
                    else
                    {
                        operation.Responses[StatusCodes.Status200OK.ToString()] = CreateSucccessResponse(ctx, true);
                        operation.Responses[StatusCodes.Status404NotFound.ToString()] = CreateNotFoundResponse();

                        ProcessKeyParameter(ctx);
                        UpdateOperation(ctx, $"Gets a {ctx.EntityType.Name} by identifier.");
                    }
                    break;

                case "GetProperty":
                    operation.Responses[StatusCodes.Status200OK.ToString()] = CreateSucccessResponse(ctx, null);
                    operation.Responses[StatusCodes.Status404NotFound.ToString()] = CreateNotFoundResponse();

                    UpdateOperation(ctx,
                        $"Gets a property value of a {ctx.EntityType.Name}.",
                        "A property value can alternatively be obtained using the $select query string parameter.");

                    ProcessKeyParameter(ctx);
                    ProcessPropertyParameter(ctx);
                    break;

                case "Post":
                    operation.RequestBody = CreateRequestBody(ctx);
                    operation.Responses[StatusCodes.Status201Created.ToString()] = CreateSucccessResponse(ctx, true);
                    operation.Responses[StatusCodes.Status400BadRequest.ToString()] = CreateBadRequestResponse();

                    UpdateOperation(ctx, $"Creates a new {ctx.EntityType.Name}.");
                    break;

                case "Put":
                case "Patch":
                    operation.RequestBody = CreateRequestBody(ctx);
                    operation.Responses[StatusCodes.Status200OK.ToString()] = CreateSucccessResponse(ctx, true);
                    operation.Responses[StatusCodes.Status204NoContent.ToString()] = CreateNoContentResponse();
                    operation.Responses[StatusCodes.Status400BadRequest.ToString()] = CreateBadRequestResponse();
                    operation.Responses[StatusCodes.Status404NotFound.ToString()] = CreateNotFoundResponse();
                    operation.Responses[StatusCodes.Status409Conflict.ToString()] = CreateConflictResponse();
                    operation.Responses[StatusCodes.Status422UnprocessableEntity.ToString()] = CreateUnprocessableEntityResponse();

                    ProcessKeyParameter(ctx);
                    UpdateOperation(ctx, ctx.ActionName == "Patch"
                        ? $"Partially updates a {ctx.EntityType.Name}."
                        : $"Updates a {ctx.EntityType.Name}.");
                    break;

                case "Delete":
                    operation.Responses[StatusCodes.Status204NoContent.ToString()] = CreateNoContentResponse();
                    operation.Responses[StatusCodes.Status404NotFound.ToString()] = CreateNotFoundResponse();

                    ProcessKeyParameter(ctx);
                    UpdateOperation(ctx, $"Deletes a {ctx.EntityType.Name}.");
                    break;
            }
        }

        protected virtual void AddQueryParameters(OpenApiOperation operation, OperationFilterContext context)
        {
            var descriptor = context.ApiDescription.ActionDescriptor as ControllerActionDescriptor;
            var queryAttribute = descriptor?.FilterDescriptors
                .Where(x => x.Filter is WebApiQueryableAttribute)
                .Select(x => x.Filter as WebApiQueryableAttribute)
                .FirstOrDefault();

            if (queryAttribute == null)
            {
                return;
            }

            var options = queryAttribute.AllowedQueryOptions;
            var parameters = operation.Parameters;

            // TODO: (mg) (core) sample input clearing is bad. Providing real working examples (per entity) is too much work. Working with input placeholder seems not to work...

            if (options.HasFlag(AllowedQueryOptions.Filter))
            {
                var schema = context.SchemaGenerator.GenerateSchema(typeof(string), context.SchemaRepository);

                var parameter = new OpenApiParameter
                {
                    Name = "$filter",
                    Description = "The filter expression(s) to limit the results to be returned.",
                    Required = false,
                    In = ParameterLocation.Query,
                    Schema = schema
                };

                parameter.Examples["Tester"] = new OpenApiExample
                {
                    Summary = "iPhone Plus",
                    Value = new OpenApiString("Name eq 'iPhone Plus'")
                };

                parameter.Examples["AnotherTester"] = new OpenApiExample
                {
                    Summary = "any product",
                    Value = new OpenApiString("Name eq 'any product'")
                };

                parameters.Add(parameter);

                //parameters.Add(CreateParameter("$filter",
                //    "The filter expression(s) to limit the results to be returned.",
                //    new OpenApiString("Name eq 'iPhone Plus'")));
            }

            if (options.HasFlag(AllowedQueryOptions.Expand))
            {
                var schema = context.SchemaGenerator.GenerateSchema(typeof(string), context.SchemaRepository);
                //schema.Default = new OpenApiString("Hello world");
                //schema.Title = "Hello world";
                //schema.Description = "Good morning";
                
                var parameter = new OpenApiParameter
                {
                    Name = "$expand",
                    Description = "A comma-separated list of related entities to be included inline in the response.",
                    Required = false,
                    In = ParameterLocation.Query,
                    Schema = schema,
                    //Example = new OpenApiString("Hello world", false)
                    //AllowEmptyValue = true
            };

                parameters.Add(parameter);
                //parameters.Add(CreateParameter("$expand",
                //    "A comma-separated list of related entities to be included inline in the response.",
                //    new OpenApiString("TierPrices")));
            }


            // TODO: (mg) (core) go on here with work.
        }

        #region Utilities

        private static OpenApiResponse CreateSucccessResponse(OperationContext ctx, bool isSingleResult)
        {
            var entityType = ctx.EntityType;
            var modelType = isSingleResult ? entityType : typeof(IQueryable<>).MakeGenericType(entityType);

            var returnStr = isSingleResult
                ? $"Returns details of {entityType.Name}."
                : $"Returns a list of {entityType.Name}.";

            if (ctx.ActionName == "Put" || ctx.ActionName == "Patch")
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
                        Schema = ctx.GenerateSchema(modelType)
                    }
                }
            };
        }

        private static OpenApiResponse CreateSucccessResponse(OperationContext ctx, Type modelType)
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
                        Schema = ctx.GenerateSchema(modelType)
                    }
                }
            };
        }

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

        private static OpenApiRequestBody CreateRequestBody(OperationContext ctx)
        {
            var required = true;
            string description = null;

            switch (ctx.ActionName)
            {
                case "Post":
                    description = $"All required fields must be provided in order to successfully create a new {ctx.EntityType.Name}.";
                    break;
                case "Put":
                    description = $"All required fields must be provided in order to successfully update a {ctx.EntityType.Name}.";
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
                        Schema = ctx.GenerateSchema(ctx.EntityType)
                    }
                }
            };
        }

        private static OpenApiParameter CreateParameter(string name, string description, IOpenApiPrimitive example)
        {
            var parameter = new OpenApiParameter
            {
                Name = name,
                Description = description,
                Required = false,
                In = ParameterLocation.Query,
                Schema = new OpenApiSchema { Type = example.PrimitiveType.ToString().ToLower(), Title = "my title", Description ="my description",
                    Example = example },
                //Example = example
            };

            return parameter;
        }

        private static void ProcessKeyParameter(OperationContext ctx)
        {
            var parameter = ctx.Op.Parameters.FirstOrDefault(x => x.Name == "key");
            var addParameter = parameter == null;

            parameter ??= new OpenApiParameter
            {
                Name = "key",
                Required = true,
                In = ParameterLocation.Path,
                Schema = ctx.GenerateSchema(typeof(int))
            };

            if (parameter.Description.IsEmpty())
            {
                parameter.Description = $"The identifier of the {ctx.EntityType.Name}.";
            }

            parameter.Example ??= new OpenApiInteger(12345);

            if (addParameter)
            {
                ctx.Op.Parameters.Add(parameter);
            }
        }

        private static void ProcessPropertyParameter(OperationContext ctx)
        {
            var parameter = ctx.Op.Parameters.FirstOrDefault(x => x.Name == "property");
            var addParameter = parameter == null;

            parameter ??= new OpenApiParameter
            {
                Name = "property",
                Required = true,
                In = ParameterLocation.Path,
                Schema = ctx.GenerateSchema(typeof(string))
            };

            if (parameter.Description.IsEmpty())
            {
                parameter.Description = "The name of the property whose value is to be returned.";
            }

            if (parameter.Example == null)
            {
                var properties = FastProperty.GetProperties(ctx.EntityType).Values;

                var prop = properties.FirstOrDefault(x => x.Property.PropertyType.Name.EqualsNoCase("String"))
                    ?? properties.FirstOrDefault(x => x.Property.PropertyType.IsBasicType())
                    ?? properties.FirstOrDefault();

                if (prop != null)
                {
                    parameter.Example = new OpenApiString(prop.Property.Name);
                }
            }

            if (addParameter)
            {
                ctx.Op.Parameters.Add(parameter);
            }
        }

        private static void UpdateOperation(OperationContext ctx, string summary, string description = null)
        {
            if (description != null && ctx.Op.Description.IsEmpty())
            {
                ctx.Op.Description = description;
            }

            if (summary != null && ctx.Op.Summary.IsEmpty())
            {
                ctx.Op.Summary = summary;
            }
        }

        #endregion

        /// <summary>
        /// Helper for better readability.
        /// </summary>
        class OperationContext
        {
            public OpenApiOperation Op { get; init; }
            public OperationFilterContext Context { get; init; }

            public string ActionName
                => Context.MethodInfo.Name.EmptyNull();

            public Type EntityType
                => Context.MethodInfo.DeclaringType.BaseType.GenericTypeArguments[0];

            public OpenApiSchema GenerateSchema(Type modelType)
                => Context.SchemaGenerator.GenerateSchema(modelType, Context.SchemaRepository);
        }
    }
}
