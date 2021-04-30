using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Web.Controllers;

namespace Smartstore.Admin.Controllers
{
    public class HomeController : AdminControllerBase
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
