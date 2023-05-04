using Smartstore.Core.Web;

namespace Smartstore.Admin.Controllers
{
    public class HomeController : AdminController
    {
        private readonly IUserAgentFactory _userAgentFactory;
        private readonly IUserAgent _userAgent;

        public HomeController(IUserAgentFactory userAgentFactory, IUserAgent userAgent)
        {
            _userAgentFactory = userAgentFactory;
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
                return View(_userAgentFactory.CreateUserAgent(ua, false));
            }
            else
            {
                return View(_userAgent);
            }
        }
    }
}
