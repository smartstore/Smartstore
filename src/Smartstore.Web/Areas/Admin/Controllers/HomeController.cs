using Smartstore.Core.Web;

namespace Smartstore.Admin.Controllers
{
    public class HomeController : AdminController
    {
        private readonly IUserAgent _userAgent;

        public HomeController(IUserAgent userAgent)
        {
            _userAgent = userAgent;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult UaTester(string ua = null)
        {
            if (ua.HasValue())
            {
                _userAgent.RawValue = ua;
            }

            return View(_userAgent);
        }
    }
}
