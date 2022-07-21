using Microsoft.AspNetCore.Mvc;
using Smartstore.Web.Controllers;

namespace Smartstore.DevTools.Controllers
{
    //[Route("MyCheckout/{action}")]
    public class MyCheckoutController : PublicController
    {
        public IActionResult MyBillingAddress()
        {
            return View();
        }
    }
}