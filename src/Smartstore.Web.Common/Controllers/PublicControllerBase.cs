using System;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;

namespace Smartstore.Web.Controllers
{
    // TODO: (core) Implement base filters for PublicControllerBase
    [AuthorizeShopAccess]
    [TrackActivity(Order = 100)]
    [SaveChanges(typeof(SmartDbContext), Order = int.MaxValue)]
    public class PublicControllerBase : SmartController
    {
    }
}
