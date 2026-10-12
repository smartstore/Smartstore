using System.IO;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Engine;
using Smartstore.ComponentModel;
using Smartstore.Core.Security;
using Smartstore.Core.Content.Media.Icons;
using Smartstore.DevTools.Models;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;

namespace Smartstore.DevTools.Controllers;

[Area("Admin")]
//[Route("module/[area]/[action]/{id?}", Name = "Smartstore.DevTools")]
public class DevToolsController(IIconCache iconCache, IIconKitService iconKits, IIconService icons, IApplicationContext applicationContext) : ModuleController
{
    [AuthorizeAdmin, Permission(DevToolsPermissions.Read)]
    [LoadSetting]
    public async Task<IActionResult> Configure(ProfilerSettings settings)
    {
        var model = MiniMapper.Map<ProfilerSettings, ConfigurationModel>(settings);
        var kitFiles = new List<IconKitFileModel>();
        var kitDirectory = Path.Combine(applicationContext.AppDataRoot.Root, ".cache", "icons", "kits");
        foreach (var kit in iconKits.Kits.OrderBy(x => x.Name, StringComparer.Ordinal))
        {
            var url = iconKits.GetUrl(kit.Name);
            if (url == null)
            {
                continue;
            }

            // Inspect only the current revision; never generate sprites for diagnostics.
            var fileName = Uri.UnescapeDataString(Path.GetFileName(url));
            var file = new FileInfo(Path.Combine(kitDirectory, fileName));
            if (file.Exists)
            {
                kitFiles.Add(new IconKitFileModel(kit.Name, fileName, file.Length));
            }
        }
        model.KitFiles = kitFiles;

        var kitAddresses = iconKits.GetSourceAddresses();
        var logoAddresses = new HashSet<string>(StringComparer.Ordinal);
        var libraries = await icons.GetLibrariesAsync(HttpContext.RequestAborted);
        var logos = libraries.SelectMany(library => library.Variants.Values
            .Select(variant => variant.Icon ?? library.Icon).Append(library.Icon))
            .Where(address => address.HasValue()).Distinct(StringComparer.Ordinal);
        foreach (var logo in logos)
        {
            // Resolve metadata only: library aliases and variant fallbacks must match
            // the canonical cache identity without loading or caching any drawing.
            var icon = await icons.GetIconAsync(logo, cancelToken: HttpContext.RequestAborted);
            if (icon != null)
            {
                logoAddresses.Add(icon.Address);
            }
        }

        var cachedIcons = new Dictionary<string, IconSvg>(StringComparer.Ordinal);
        await foreach (var entry in iconCache.GetEntriesAsync(HttpContext.RequestAborted))
        {
            var address = entry.Address.ToString();
            if (kitAddresses.Contains(address) || logoAddresses.Contains(address) || cachedIcons.ContainsKey(address))
            {
                continue;
            }

            // Entries can expire between enumeration and lookup. Read the cached payload
            // directly so this diagnostic view does not populate or refresh the icon cache.
            var svg = await iconCache.GetAsync(entry.Key, HttpContext.RequestAborted);
            if (svg != null)
            {
                cachedIcons.TryAdd(svg.Address, svg);
            }
        }

        model.CachedIcons = cachedIcons.Values.OrderBy(x => x.Address, StringComparer.Ordinal).ToArray();
        return View(model);
    }

    [AuthorizeAdmin, Permission(DevToolsPermissions.Update)]
    [HttpPost, SaveSetting]
    public async Task<IActionResult> Configure(ConfigurationModel model, ProfilerSettings settings)
    {
        if (!ModelState.IsValid)
        {
            return await Configure(settings);
        }

        ModelState.Clear();
        MiniMapper.Map(model, settings);

        return RedirectToAction(nameof(Configure));
    }

    [AuthorizeAdmin]
    public IActionResult ProductEditTab(int productId)
    {
        var model = new BackendExtensionModel
        {
            Welcome = "Hello world!"
        };

        ViewData.TemplateInfo.HtmlFieldPrefix = "CustomProperties[DevTools]";
        return View(model);
    }
}