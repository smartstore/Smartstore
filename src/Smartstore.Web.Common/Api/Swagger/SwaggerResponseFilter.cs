using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Smartstore.Web.Api.Security;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Smartstore.Web.Api.Swagger
{
    /// <summary>
    /// Filters endpoints by HTTP method, return type etc. and adds describing <see cref="OpenApiResponse"/> objects for Swagger documentation.
    /// Only takes into account classes that inherit from <see cref="SmartODataController<>"/>.
    /// </summary>
    public class SwaggerResponseFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            try
            {
                var mi = context.MethodInfo;
                var classBaseType = mi.DeclaringType.BaseType;
                //var method = context.ApiDescription.HttpMethod;

                // Skip what is not inherited from our SmartODataController.
                if (classBaseType.GetGenericTypeDefinition() != typeof(SmartODataController<>))
                {
                    return;
                }

                // TODO: (mg) (core) what about "application/problem+json" for 40x?

                if (mi.Name.EqualsNoCase("Get"))
                {
                    operation.Responses.Clear();

                    if (mi.ReturnType.IsGenericType && mi.ReturnType.GetGenericTypeDefinition() == typeof(IQueryable<>))
                    {
                        operation.Responses.Add(StatusCodes.Status200OK.ToString(), CreateSucccessResponse(false, context));
                    }
                    else
                    {
                        operation.Responses.Add(StatusCodes.Status200OK.ToString(), CreateSucccessResponse(true, context));
                        operation.Responses.Add(StatusCodes.Status404NotFound.ToString(), CreateNotFoundResponse());
                    }
                }
                else if (mi.Name.EqualsNoCase("GetProperty"))
                {
                    operation.Responses.Clear();

                    operation.Responses.Add(StatusCodes.Status200OK.ToString(), CreateSucccessResponse(null, context));
                    operation.Responses.Add(StatusCodes.Status404NotFound.ToString(), CreateNotFoundResponse());
                }

                // TODO: (mg) (core) go on with work...

                if (mi.DeclaringType.GetAttributes<AuthorizeAttribute>(true).Any() || mi.GetAttributes<AuthorizeAttribute>(true).Any())
                {
                    operation.Responses.Add(StatusCodes.Status401Unauthorized.ToString(), CreateUnauthorizedResponse());
                }
            }
            catch
            {
            }
        }

        private static OpenApiResponse CreateSucccessResponse(bool isSingleResult, OperationFilterContext context)
        {
            var entityType = context.MethodInfo.DeclaringType.BaseType.GenericTypeArguments[0];
            var modelType = isSingleResult ? entityType : typeof(IQueryable<>).MakeGenericType(entityType);

            var returnStr = isSingleResult
                ? $"Returns details of {entityType.Name}."
                : $"Returns a list of {entityType.Name}.";

            return new OpenApiResponse
            {
                Description = $"The request has succeeded. {returnStr}",
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    [MediaTypeNames.Application.Json] = new OpenApiMediaType
                    {
                        Schema = context.SchemaGenerator.GenerateSchema(modelType, context.SchemaRepository)
                    }
                }
            };
        }

        private static OpenApiResponse CreateSucccessResponse(Type modelType, OperationFilterContext context)
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
                        Schema = context.SchemaGenerator.GenerateSchema(modelType, context.SchemaRepository)
                    }
                }
            };
        }

        private static OpenApiResponse CreateUnauthorizedResponse()
            => new() { Description = $"Unauthorized API request. The exact reason is provided by the {BasicAuthenticationHandler.ResultDescriptionHeader} response header." };

        private static OpenApiResponse CreateNotFoundResponse()
            => new() { Description = "The requested resource was not found." };
    }
}
