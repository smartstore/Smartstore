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

        [JsonProperty("requestId")]
        public string RequestId { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("statusCode")]
        public HttpStatusCode StatusCode { get; set; }

        [JsonProperty("message")]
        public string ErrorMessage => Exception?.Message;

        [JsonProperty("controller")]
        public string ControllerName  => ActionDescriptor?.ControllerName;

        [JsonProperty("action")]
        public string ActionName => ActionDescriptor?.ActionName;
    }
}