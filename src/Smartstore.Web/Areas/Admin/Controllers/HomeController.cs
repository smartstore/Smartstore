using System.Text.Encodings.Web;
using System.Xml.Linq;
using Smartstore.Core.Content.Media.Icons;
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

    /// <summary>
    /// Renders an icon address for the migration comparison editor. Unqualified names remain literal HI proposals.
    /// </summary>
    /// <param name="name">An icon name or address, including optional variant and query modifiers.</param>
    /// <param name="icons">Resolves the proposal using the shared icon catalog.</param>
    /// <param name="renderer">Renders the resolved SVG preview.</param>
    /// <param name="cancelToken">The request cancellation token.</param>
    [HttpGet]
    public async Task<IActionResult> IconCheatsheetPreview(string name,
        [FromServices] IIconService icons, [FromServices] IIconRenderer renderer, CancellationToken cancelToken)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 512)
        {
            return BadRequest();
        }

        try
        {
            var separator = name.IndexOf('?');
            var address = IconAddress.Parse(separator < 0 ? name : name[..separator]);
            if (address.Library == null)
            {
                // Keep the original editor's literal HI semantics, including explicit variant choices.
                address = new IconAddress(address.Name, "hi", address.Variant ?? "sr", skipMapping: true);
                name = address.ToString() + (separator < 0 ? string.Empty : name[separator..]);
            }

            var icon = await icons.GetIconAsync(name, cancelToken: cancelToken);
            var svg = icon == null ? null : await renderer.RenderAsync(icon, cancelToken: cancelToken);
            if (svg == null)
            {
                return Content(string.Empty, "text/html");
            }

            using var writer = new StringWriter();
            svg.WriteTo(writer, HtmlEncoder.Default);
            return Content(writer.ToString(), "text/html");
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
        catch (FormatException)
        {
            return BadRequest();
        }
        catch (InvalidDataException)
        {
            return BadRequest();
        }
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
