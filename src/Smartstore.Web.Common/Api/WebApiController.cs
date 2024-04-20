using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.OData.Routing.Attributes;

namespace Smartstore.Web.Api
{
    [ODataRouteComponent("odata/v1")]
    [Route("odata/v1")]
    [EnableCors("WebApiCorsPolicy")]
    public abstract class WebApiController<TEntity> : SmartODataController<TEntity>
        where TEntity : BaseEntity, new()
    {
    }
}
