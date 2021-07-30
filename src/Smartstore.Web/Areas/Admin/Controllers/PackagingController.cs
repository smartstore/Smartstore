using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Packaging;
using Smartstore.Core.Security;
using Smartstore.Core.Theming;
using Smartstore.Engine.Modularity;
using Smartstore.Web.Controllers;

namespace Smartstore.Controllers
{
    public class PackagingController : AdminControllerBase
    {
        private readonly IPackageInstaller _packageInstaller;
        private readonly IPackageBuilder _packageBuilder;
        private readonly IThemeRegistry _themeRegistry;

        public PackagingController(IPackageInstaller packageInstaller, IPackageBuilder packageBuilder, IThemeRegistry themeRegistry)
        {
            _packageInstaller = packageInstaller;
            _packageBuilder = packageBuilder;
            _themeRegistry = themeRegistry;
        }

        public async Task<IActionResult> BuildPackage(string theme)
        {
            // TODO: (core) Remove later
            var themeDescriptor = _themeRegistry.GetThemeDescriptor(theme);
            using var package = await _packageBuilder.BuildPackageAsync(themeDescriptor);

            return File(package.ArchiveStream, "application/zip", package.FileName);
        }

        [HttpPost]
        public async Task<IActionResult> UploadPackage(bool expectTheme, string returnUrl = null)
        {
            returnUrl ??= Services.WebHelper.GetUrlReferrer()?.OriginalString;

            var message = (string)null;
            var file = (IFormFile)null;

            if (Request.Form.Files.Count == 0)
            {
                message = T("Admin.Common.UploadFile").Value;
                return Json(new { fileName = file.FileName, message, returnUrl });
            }
            
            try
            {
                file = Request.Form.Files[0];

                if (!Path.GetExtension(file.FileName).EqualsNoCase(".zip"))
                {
                    message = T("Admin.Packaging.NotAPackage").Value;
                    return Json(new { fileName = file.FileName, message, returnUrl });
                }

                using var package = new ExtensionPackage(file.OpenReadStream(), false) { FileName = file.Name };

                var isTheme = package.Descriptor.ExtensionType == ExtensionType.Theme;
                if (isTheme != expectTheme)
                {
                    message = T("Admin.Packaging." + (isTheme ? "NotAModule" : "NotATheme")).Value;
                    return Json(new { fileName = file.FileName, message, returnUrl });
                }

                var requiredPermission = isTheme ? Permissions.Configuration.Theme.Upload : Permissions.Configuration.Module.Upload;

                if (!await Services.Permissions.AuthorizeAsync(requiredPermission))
                {
                    message = T("Admin.AccessDenied.Description").Value;
                    return Json(new { fileName = file.FileName, message, returnUrl });
                }

                // ===> Install package now
                var result = await _packageInstaller.InstallPackageAsync(package);

                message = T("Admin.Packaging.InstallSuccess" + (isTheme ? ".Theme" : ""), result.Name).ToString();
                return Json(new { success = true, file = file.Name, message, returnUrl });
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Json(new { fileName = file?.Name, message, returnUrl });
            }
        }
    }
}
