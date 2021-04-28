using System;
using Smartstore.Core.Checkout.Affiliates;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;

namespace Smartstore.Web.Controllers
{
    [AuthorizeShopAccess]
    [TrackActivity(Order = 100)]
    [CheckAffiliate(Order = 100)]
    [SaveChanges(typeof(SmartDbContext), Order = int.MaxValue)]
    public class PublicControllerBase : SmartController
    {
    }
}
