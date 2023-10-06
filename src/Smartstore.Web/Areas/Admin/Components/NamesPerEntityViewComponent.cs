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
            ViewBag.UrlRecordListUrl = Url.Action("Index", "UrlRecord", new { entityName, entityId, area = "Admin" });

            return View();
        }
    }
}
