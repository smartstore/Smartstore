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

        // Example of how to add a route menu item for this action method under CMS > Menus.
        // - Add menu item > Route
        // - Target (Route name): Smartstore.DevTools.MenuItem
        // - Route values (JSON): {"id":16,"name":"hello world!"}
        //[Route("/mycheckout/menuitem/{id?}", Name = "Smartstore.DevTools.MenuItem")]
        //public IActionResult MenuItem(int id, string name)
        //{
        //    return Content($"DevTools Menu Item... id:{id} name:{name}");
        //}
    }
}