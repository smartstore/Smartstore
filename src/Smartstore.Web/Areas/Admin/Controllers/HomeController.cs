using Smartstore.Core.Web;

namespace Smartstore.Admin.Controllers
{
    public class HomeController : AdminController
    {
        private readonly IUserAgent _userAgent;
        private readonly IUserAgent2 _userAgent2;

        public HomeController(IUserAgent userAgent, IUserAgent2 userAgent2)
        {
            _userAgent = userAgent;
            _userAgent2 = userAgent2;
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
                _userAgent2.RawValue = ua;
            }

            return View(_userAgent);
        }
    }
}
