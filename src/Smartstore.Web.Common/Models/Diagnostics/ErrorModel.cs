using System;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Newtonsoft.Json;

namespace Smartstore.Web.Models.Diagnostics
{
    public class ErrorModel
    {
        [JsonIgnore]
        public Exception Exception { get; set; }

        [JsonIgnore]
        public Endpoint Endpoint { get; set; }

        [JsonIgnore]
        public ControllerActionDescriptor ActionDescriptor { get; set; }

        public string RequestId { get; set; }
        public string Path { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string ErrorMessage => Exception?.Message;
        public string ControllerName  => ActionDescriptor?.ControllerName;
        public string ActionName => ActionDescriptor?.ActionName;
    }
}