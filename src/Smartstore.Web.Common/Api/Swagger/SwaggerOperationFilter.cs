using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
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

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            try
            {
                var mi = context.MethodInfo;
                var isCandidate = _candidateMethodNames.Contains(mi.Name);

                // Skip what is not inherited from our SmartODataController.
                // INFO: (mg) (core) GetGenericTypeDefinition() throws if arg is not a generic type
                if (!mi.DeclaringType.BaseType.IsClosedGenericTypeOf(typeof(SmartODataController<>)))
                {
                    return;
                }     

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
            catch
            {
            }
        }

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
