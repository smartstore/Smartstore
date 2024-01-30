using System.Diagnostics;
using System.Net;
using System.Net.Mime;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Smartstore.Core.Security;
using Smartstore.Core.Seo.Routing;
using Smartstore.IO;
using Smartstore.Web.Models.Diagnostics;

namespace Smartstore.Web.Controllers
{
    public class ErrorController : Controller
    {
        [Route("/error/{status?}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? status)
        {
            Enum.TryParse((status ?? HttpContext.Response.StatusCode).ToString(), true, out HttpStatusCode httpStatusCode);

            var errorFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            var reExecuteFeature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
            var urlPolicy = HttpContext.GetUrlPolicy();
            
            var model = new ErrorModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                StatusCode = httpStatusCode,
                Exception = errorFeature?.Error,
                Path = errorFeature?.Path ?? (reExecuteFeature?.OriginalPath + reExecuteFeature?.OriginalQueryString).NullEmpty(),
                Endpoint = urlPolicy.Endpoint
            };

            if (model.Endpoint != null)
            {
                // Set the original action descriptor.
                model.ActionDescriptor = model.Endpoint.Metadata.OfType<ControllerActionDescriptor>().FirstOrDefault();
            }

            if (Request.IsAjax() || Request.Headers.Accept.Any(x => x.StartsWithNoCase(MediaTypeNames.Application.Json)))
            {
                var json = model.Exception?.Data["JsonContent"]?.ToString();
                if (json.HasValue())
                {
                    // Return JSON content instead of our ErrorModel.
                    return Content(json, MediaTypeNames.Application.Json);
                }

                return Json(model);
            }

            if (model.Exception is AccessDeniedException)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;

                return View("AccessDenied", model);
            }

            if (httpStatusCode == HttpStatusCode.NotFound && model.Path.HasValue() && MimeTypes.TryMapNameToMimeType(model.Path, out var mime))
            {
                HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;

                return Content("Resource not found", mime);
            }

            switch (httpStatusCode)
            {
                case HttpStatusCode.NotFound:
                    return View("NotFound", model);
                default:
                    return View("Error", model);
            }
        }
    }
}
