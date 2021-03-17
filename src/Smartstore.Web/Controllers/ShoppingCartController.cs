using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Smartstore.Web.Controllers
{
    public class ShoppingCartController : PublicControllerBase
    {
        public IActionResult CartSummary()
        {
            // Stop annoying MiniProfiler report.
            return new EmptyResult();
        }
    }
}
