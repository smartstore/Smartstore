using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Data;
using Smartstore.Web.Controllers;
using System.Threading.Tasks;

namespace Smartstore.Web.Areas.Admin.Controllers
{
    public class CommonController : AdminControllerBase
    {
        private readonly SmartDbContext _db;

        public CommonController(SmartDbContext db)
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
