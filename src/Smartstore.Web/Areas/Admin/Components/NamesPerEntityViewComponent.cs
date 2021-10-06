using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Web.Components;

namespace Smartstore.Admin.Components
{
    public class NamesPerEntityViewComponent : SmartViewComponent
    {
        private readonly SmartDbContext _db;

        public NamesPerEntityViewComponent(SmartDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync(string entityName, int entityId)
        {
            // Permission check not necessary.
            if (entityName.IsEmpty() || entityId == 0)
            {
                return Empty();
            }

            var numRecords = await _db.UrlRecords
                .Where(x => x.EntityName == entityName && x.EntityId == entityId)
                .CountAsync();
            
            ViewBag.CountSlugsPerEntity = numRecords;

            // TODO: (mh) (core) Implement action UrlRecord > List.
            ViewBag.UrlRecordListUrl = Url.Action("List", "UrlRecord", new { entityName, entityId, area = "Admin" });

            return View();
        }
    }
}
