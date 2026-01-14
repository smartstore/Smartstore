using System.Net;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;

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

        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("statusCode")]
        public HttpStatusCode StatusCode { get; set; }

        [JsonPropertyName("message")]
        public string ErrorMessage => Exception?.Message;

        [JsonPropertyName("controller")]
        public string ControllerName => ActionDescriptor?.ControllerName;

        [JsonPropertyName("action")]
        public string ActionName => ActionDescriptor?.ActionName;
    }
}