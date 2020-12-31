using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Smartstore.Engine;
using Smartstore.Web.UI.TagHelpers;

namespace Smartstore.Web.Controllers
{
    public class StateController : Controller
    {
        private readonly IApplicationContext _appContext;
        
        public StateController(IApplicationContext appContext)
        {
            _appContext = appContext;
        }

        [HttpPost]
        public IActionResult SetSelectedTab(string navId, string tabId, string path)
        {
            if (navId.HasValue() && tabId.HasValue() && path.HasValue())
            {
                var info = new SelectedTabInfo { TabId = tabId, Path = path };
                TempData["SelectedTab." + navId] = info;
            }

            return Json(new { Success = true });
        }


        [HttpPost]
        public IActionResult SetGridState(string gridId, string state, string path)
        {
            //// TODO: (core) Implement StateController.SetGridState()

            //if (gridId.HasValue() && state != null && path.HasValue())
            //{
            //    var info = new GridStateInfo { State = state, Path = path };
            //    TempData[gridId] = info;
            //}

            return Json(new { Success = true });
        }
    }
}
