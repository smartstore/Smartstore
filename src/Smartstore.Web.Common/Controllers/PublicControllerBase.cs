using System;
using Smartstore.Core.Data;

namespace Smartstore.Web.Controllers
{
    // TODO: (core) Implement base filters for PublicControllerBase
    [SaveChanges(typeof(SmartDbContext))]
    public class PublicControllerBase : SmartController
    {
    }
}
