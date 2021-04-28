using System;
using Smartstore.Core.Checkout.Affiliates;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;

namespace Smartstore.Web.Controllers
{
    [CheckStoreClosed]
    [AuthorizeShopAccess]
    [TrackActivity(Order = 100)]
    [CheckAffiliate(Order = 100)]
    [SaveChanges(typeof(SmartDbContext), Order = int.MaxValue)]
    public class PublicControllerBase : SmartController
    {
    }
}
