using System.Net;
using System.Runtime.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Newtonsoft.Json;

namespace Smartstore.Web.Models.Diagnostics
{
    public class ErrorModel
    {
        [IgnoreDataMember]
        public Exception Exception { get; set; }

        [IgnoreDataMember]
        public Endpoint Endpoint { get; set; }

        [IgnoreDataMember]
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
        public string ControllerName => ActionDescriptor?.ControllerName;

        [JsonProperty("action")]
        public string ActionName => ActionDescriptor?.ActionName;
    }
}