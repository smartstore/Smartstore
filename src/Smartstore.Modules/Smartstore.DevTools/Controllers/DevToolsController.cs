using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Web.Controllers;

namespace Smartstore.DevTools.Controllers
{
    [Area("Admin")]
    //[Route("module/[area]/[action]/{id?}", Name = "Smartstore.DevTools")]
    public class DevToolsController : ModuleController
    {
        public IActionResult Configure()
        {
            return View();
        }
    }
}
