using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Smartstore.Web.Controllers;
using Smartstore.Core.Data;

namespace Smartstore.Web.Areas.Admin.Controllers
{
    public class LanguageController : AdminController
    {
        private readonly SmartDbContext _db;

        public LanguageController(SmartDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> LanguageSelected(int customerlanguage)
        {
            var language = await _db.Languages.FindByIdAsync(customerlanguage, false);
            if (language != null && language.Published)
            {
                Services.WorkContext.WorkingLanguage = language;
            }

            return Content(T("Admin.Common.DataEditSuccess"));
        }
    }
}
