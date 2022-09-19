using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using OpenApiOpType = Microsoft.OpenApi.Models.OperationType;

namespace Smartstore.Web.Api.Swagger
{
    //public class SwaggerDocumentFilter : IDocumentFilter
    //{
    //    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    //    {
    //        //var smartControllerType = typeof(SmartODataController<>);

    //        foreach (var description in context.ApiDescriptions)
    //        {
    //            var descriptor = (ControllerActionDescriptor)description.ActionDescriptor;
    //            //var isSub = descriptor.ControllerTypeInfo.IsSubclassOf(smartControllerType);
    //            //$"{isSub} {descriptor.ControllerName}".Dump();

    //            if (descriptor.ControllerTypeInfo.GetCustomAttributes(typeof(SwaggerDocumentAttribute), true).Any() ||
    //                descriptor.MethodInfo.GetCustomAttributes(typeof(SwaggerDocumentAttribute), true).Any())
    //            {
    //            }
    //            else
    //            {
    //                var key = "/" + description.RelativePath.TrimEnd('/');
    //                var operation = (OpenApiOpType)Enum.Parse(typeof(OpenApiOpType), description.HttpMethod, true);

    //                $"{operation} {key}".Dump();
    //            }
    //        }
    //    }
    //}
}
