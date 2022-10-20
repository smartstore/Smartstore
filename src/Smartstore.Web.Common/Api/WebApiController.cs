using Microsoft.AspNetCore.OData.Routing.Attributes;

namespace Smartstore.Web.Api
{
    [ODataRouteComponent("odata/v1")]
    [Route("odata/v1")]
    [ApiController, ApiExplorerSettings(GroupName = "webapi1")]
    // TODO: (mg) (core) Check the benefits of ApiControllerAttribute for OData:
    // https://www.strathweb.com/2018/02/exploring-the-apicontrollerattribute-and-its-features-for-asp-net-core-mvc-2-1/.
    // Investigate what code parts can be removed/simplified when using the attribute (at least [FromBody] seems to be obsolete now).
    // Furthermore check the impact on API exploration.
    public abstract class WebApiController<TEntity> : SmartODataController<TEntity>
        where TEntity : BaseEntity, new()
    {
    }
}
