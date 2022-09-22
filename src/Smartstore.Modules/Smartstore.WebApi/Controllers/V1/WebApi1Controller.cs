using Microsoft.AspNetCore.OData.Routing.Attributes;
using Smartstore.Domain;

namespace Smartstore.Web.Api.Controllers.V1
{
    [ODataRouteComponent("odata/v1")]
    [ApiExplorerSettings(GroupName = "webapi1")]
    public abstract class WebApi1Controller<TEntity> : SmartODataController<TEntity>
        where TEntity : BaseEntity, new()
    {
    }
}
