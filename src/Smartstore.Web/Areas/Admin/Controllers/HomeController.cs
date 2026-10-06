using System.Xml.Linq;
using Smartstore.Core.Web;

namespace Smartstore.Admin.Controllers;

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

    /// <summary>
    /// Displays an unlisted comparison of icon libraries for admin UI evaluation.
    /// </summary>
    [HttpGet]
    public IActionResult IconCheatsheet()
    {
        var file = Services.ApplicationContext.ContentRoot.GetFile("/Areas/Admin/sitemap.xml");
        var sitemap = XDocument.Load(file.PhysicalPath);
        return View(sitemap.Root.Element("siteMapNode"));
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
