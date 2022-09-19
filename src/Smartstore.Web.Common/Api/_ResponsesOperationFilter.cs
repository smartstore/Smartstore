using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Smartstore.Web.Api.Security;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Smartstore.Web.Api
{
    public class ResponsesOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            try
            {
                var authAttributes = context.MethodInfo.DeclaringType.GetCustomAttributes(true)
                    .Union(context.MethodInfo.GetCustomAttributes(true))
                    .OfType<AuthorizeAttribute>();

                if (authAttributes.Any())
                {
                    operation.Responses.Add(StatusCodes.Status401Unauthorized.ToString(), new OpenApiResponse
                    {
                        Description = $"Unauthorized API request. The exact reason is provided by the {BasicAuthenticationHandler.ResultDescriptionHeader} response header."
                    });
                }
            }
            catch
            {
            }
        }
    }
}
