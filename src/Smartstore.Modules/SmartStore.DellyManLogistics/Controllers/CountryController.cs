using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore;
using Smartstore.Core;
using Smartstore.Web.Controllers;
using SmartStore.DellyManLogistics.Client;

namespace SmartStore.DellyManLogistics.Controllers
{
    public class CountryController : PublicController
    {
        private readonly IDellyManClient _dellyManClient;
        private readonly ICommonServices _commonServices;

        public CountryController(IDellyManClient dellyManClient,
            ICommonServices commonServices)
        {
            _dellyManClient = dellyManClient;
            _commonServices = commonServices;
        }

        /// <summary>
        /// This action method gets called via an ajax request.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> GetStatesByCountryId(string countryId, bool addEmptyStateIfRequired)
        {
            // This should never happen. But just in case we return an empty List to don't throw in frontend.
            //if (!countryId.HasValue())
            //{
                return Json(new List<SelectListItem>());
            //}

            //var stateProvince = _commonServices.StoreContext.CurrentStore.pro _stateProvinceService.GetStateProvinceById(int.Parse(countryId));
            //var states = await _dellyManClient.GetStatesAsync();
            //var selectedState = states.FirstOrDefault(s => s.Name == stateProvince.Name);

            //var cities = await _dellyManClient.GetCitiesAsync(selectedState.StateID);

            //var data = cities.Select(c => new { id = c.CityID, name = c.Name }).ToList();
            //return Json(data);

        }

    }
}
