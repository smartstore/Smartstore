using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Smartstore.Core.Seo.Routing;
using Smartstore.Web.Models.Diagnostics;

namespace Smartstore.Web.Controllers
{
    public class ErrorController : Controller
    {
        private readonly UrlPolicy _urlPolicy;
        private readonly ILoggerFactory _loggerFactory;

        public ErrorController(UrlPolicy urlPolicy, ILoggerFactory loggerFactory)
        {
            _urlPolicy = urlPolicy;
            _loggerFactory = loggerFactory;
        }

        [Route("/Error/{status?}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? status)
        {
            Enum.TryParse((status ?? 500).ToString(), true, out HttpStatusCode httpStatusCode);

            var errorFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            var reExecuteFeature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
            var isUnauthorizedAccessException = errorFeature?.Error is UnauthorizedAccessException;

            var model = new ErrorModel 
            { 
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                StatusCode = httpStatusCode,
                Exception = errorFeature?.Error,
                Path = errorFeature?.Path ?? (reExecuteFeature?.OriginalPath + reExecuteFeature?.OriginalQueryString).NullEmpty(),
                Endpoint = _urlPolicy.Endpoint
            };

            //if (httpStatusCode == HttpStatusCode.Unauthorized && Request.Headers.TryGetValue("PermissionSystemName", out var permissionSystemName))
            //{
            //    var message = _permissionService.Value.GetUnauthorizedMessageAsync(permissionSystemName).Await();
            //    message.Dump();
            //}

            if (model.Endpoint != null)
            {
                // Set the original action descriptor
                model.ActionDescriptor = model.Endpoint.Metadata.OfType<ControllerActionDescriptor>().FirstOrDefault();
            }

            TryLogError(model);

            if (Request.IsAjaxRequest())
            {
                return Json(model);
            }

            switch (httpStatusCode)
            {
                case HttpStatusCode.NotFound:
                    return View("NotFound", model);
                //case HttpStatusCode.Unauthorized:
                //    return View("Unauthorized", model);
                default:
                    if (isUnauthorizedAccessException)
                    {
                        return View("Unauthorized", model);
                    }

                    return View("Error", model);
            }
        }
        
        private void TryLogError(ErrorModel model)
        {
            if (model.Exception == null)
                return;

            if (model.Exception.IsFatal())
                return;

            if (model.StatusCode < HttpStatusCode.InternalServerError)
                return;

            try
            {
                var logger = model.ActionDescriptor != null 
                    ? _loggerFactory.CreateLogger(model.ActionDescriptor.ControllerTypeInfo.AsType())
                    : _loggerFactory.CreateLogger<ErrorController>();

                logger.Error(model.Exception);
            }
            catch
            {
                // Don't throw new exception
            }
        }
    }
}
