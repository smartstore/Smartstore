using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Identity;
using Smartstore.Web.Models.Identity;

namespace Smartstore.Web.Components
{
    //TODO: (mh) (core) Remove component once auth modules are available, which can render directly into the zone via PublicInfo methods or something similiar
    public class ExternalAuthentication : SmartViewComponent
    {
        private readonly SignInManager<Customer> _signInManager;

        public ExternalAuthentication(SignInManager<Customer> signInManager)
        {
            _signInManager = signInManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var model = new List<ExternalAuthenticationMethodModel>();
            var methods = await _signInManager.GetExternalAuthenticationSchemesAsync();

            foreach(var method in methods)
            {
                model.Add(new ExternalAuthenticationMethodModel { 
                    ProviderName = method.Name,
                    DisplayName = method.DisplayName
                });
            }

            return View(model);
        }
    }
}
