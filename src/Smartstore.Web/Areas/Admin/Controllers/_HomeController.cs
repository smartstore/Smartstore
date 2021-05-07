using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Web.Areas.Admin.Models;
using Smartstore.Web.Controllers;

namespace Smartstore.Admin.Controllers
{
    public class HomeController : AdminControllerBase
    {
        public IActionResult Index()
        {
            return View();
        }

        // TODO: (mh) (core) Remove when testing is done.
        public IActionResult EditorTemplates()
        {
            var model = new EditorTemplatesTestModel();
            model.Color = "#ffeedd";
            model.Media = 6;
            model.Address.CompanyEnabled = true;
            model.Address.CountryEnabled = true;
            model.Address.StateProvinceEnabled = true;
            model.Address.CityEnabled = true;
            model.Address.CityRequired = true;
            model.Address.StreetAddressEnabled = true;
            model.Address.StreetAddressRequired = true;
            model.Address.StreetAddress2Enabled = true;
            model.Address.ZipPostalCodeEnabled = true;
            model.Address.ZipPostalCodeRequired = true;
            model.Address.PhoneEnabled = true;
            model.Address.PhoneRequired = true;
            model.Address.FaxEnabled = true;

            return View(model);
        }

        [HttpPost]
        public IActionResult EditorTemplates(EditorTemplatesTestModel model)
        {

            return View(model);
        }
    }
}
