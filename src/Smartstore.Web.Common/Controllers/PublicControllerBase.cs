using System;
using Smartstore.Core.Data;
using Smartstore.Core.Security;

namespace Smartstore.Web.Controllers
{
    // TODO: (core) Implement base filters for PublicControllerBase
    [AuthorizeShopAccess]
    [SaveChanges(typeof(SmartDbContext))]
    public class PublicControllerBase : SmartController
    {
    }
}
