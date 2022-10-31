using Microsoft.AspNetCore.Diagnostics;

namespace Smartstore.Web.Api
{
    internal class ODataExceptionHandlerPathFeature : IExceptionHandlerPathFeature
    {
        public ODataExceptionHandlerPathFeature(Exception error, HttpRequest request)
        {
            Error = error;
            Path = request?.Path;
        }

        public Exception Error { get; }
        public string Path { get; }
    }
}
