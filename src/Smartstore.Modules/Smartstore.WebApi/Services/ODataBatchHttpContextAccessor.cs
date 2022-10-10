namespace Smartstore.Web.Api
{
    /// <summary>
    /// Fixes <c>null</c> for IHttpContextAccessor.HttpContext when executing odata batch items.
    /// Needs to be placed after UseODataBatching. See
    /// https://github.com/dotnet/aspnet-api-versioning/issues/633
    /// https://github.com/OData/WebApi/issues/2294
    /// </summary>
    public class ODataBatchHttpContextAccessor
    {
        private readonly RequestDelegate _next;

        public ODataBatchHttpContextAccessor(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext, IHttpContextAccessor contextAccessor)
        {
            contextAccessor.HttpContext ??= httpContext;

            await _next(httpContext);
        }
    }
}
